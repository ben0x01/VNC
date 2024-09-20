using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    const int MOUSEEVENTF_LEFTDOWN = 0x02;
    const int MOUSEEVENTF_LEFTUP = 0x04;

    static async Task Main(string[] args)
    {
        string serverUrl = "";
        string tcpServerIp = "";
        int tcpServerPort = ;

        _ = Task.Run(() => ReceiveCoordinatesFromTCPServer(tcpServerIp, tcpServerPort));

        while (true)
        {
            await CaptureAndSendScreenshot(serverUrl);
            await Task.Delay(100);
        }
    }

    static async Task ReceiveCoordinatesFromTCPServer(string ip, int port)
    {
        try
        {
            while (true)
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ip, port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] buffer = new byte[1024];
                        StringBuilder coordinates = new StringBuilder();

                        while (true)
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                                break;
                            string coords = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                            Console.WriteLine("Received coordinates: " + coords);

                            string[] coordArray = coords.Split(',');
                            int x = int.Parse(coordArray[0]);
                            int y = int.Parse(coordArray[1]);

                            SetCursorPos(x, y);

                            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, x, y, 0, 0);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving coordinates: {ex.Message}");
        }
    }

    static async Task CaptureAndSendScreenshot(string serverUrl)
    {
        try
        {
            Bitmap screenshot = CaptureScreen();
            byte[] screenshotBytes = GetBytesFromImage(screenshot);
            string screenshotString = Convert.ToBase64String(screenshotBytes);

            await SendBase64ToServer(screenshotString, serverUrl);

            //Console.WriteLine("Screenshot sent successfully!");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"Failed to send screenshot: {ex.Message}");
        }
    }

    static byte[] GetBytesFromImage(Bitmap image)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            image.Save(ms, ImageFormat.Jpeg);
            return ms.ToArray();
        }
    }

    static Bitmap CaptureScreen()
    {
        //Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
        Bitmap screenshot = new Bitmap(1280, 768, PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(screenshot))
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(1280, 768), CopyPixelOperation.SourceCopy);

        return screenshot;
    }

    static async Task SendBase64ToServer(string base64Data, string serverUrl)
    {
        try
        {
            string apiUrl = $"{serverUrl}/upload_screenshot";

            var payload = new { screenshot = base64Data };
            string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Failed to send screenshot to server. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending screenshot to server: {ex.Message}");
        }
    }
}

