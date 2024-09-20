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

    static async Task Main()
    {
        string serverUrl = "";

        TcpClient client = await ConnectToTcpSocket();
        NetworkStream imageStream = await CreateImageStream(client);

        _ = Task.Run(() => ReceiveCoordinatesFromTCPServer(client, imageStream));

        while (true)
        {
            await CaptureAndSendScreenshot(serverUrl, imageStream);
            //await Task.Delay(10);
        }
    }

    static async Task<TcpClient> ConnectToTcpSocket()
    {
        string tcpServerIp = "";
        int tcpServerPort = ;

        try
        {
            TcpClient client = new TcpClient();
            await client.ConnectAsync(tcpServerIp, tcpServerPort);

            return client;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");

            return null;
        }
    }

    static async Task<NetworkStream> CreateImageStream(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();

            return stream;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed creating a stream: ", ex);

            client.Close();

            return null;
        }
    }

    static async Task ReceiveCoordinatesFromTCPServer(TcpClient client, NetworkStream stream)
    {
        try
        {
            while (true)
            {
                byte[] buffer = new byte[1024];
                StringBuilder coordinates = new StringBuilder();

                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;
                    string cords = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Received coordinates: " + cords);

                    int[] cordArray = GetCoordArray(cords.Split(','));

                    SetCursorPos(cordArray[0], cordArray[1]);

                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, cordArray[0], cordArray[1], 0, 0);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving coordinates: {ex.Message}");
        }
    }

    static int[] GetCoordArray(string[] strCoordArray)
    {
        int decimalIndexX = strCoordArray[0].IndexOf('.');
        int decimalIndexY = strCoordArray[1].IndexOf('.');

        if (decimalIndexX < 0 && decimalIndexY < 0)
            return new int[] { int.Parse(strCoordArray[0]), int.Parse(strCoordArray[1]) };

        return new int[] { decimalIndexX > 0 ? int.Parse(strCoordArray[0].Substring(decimalIndexX)) : int.Parse(strCoordArray[0]),
        decimalIndexY > 0 ? int.Parse(strCoordArray[1].Substring(decimalIndexY)) : int.Parse(strCoordArray[1])
        };
    }

    static async Task CaptureAndSendScreenshot(string serverUrl, NetworkStream stream)
    {
        try
        {
            Bitmap screenshot = CaptureScreen();
            byte[] screenshotBytes = GetBytesFromImage(screenshot);

            await SendBytesToServer(screenshotBytes, serverUrl, stream);

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
        Bitmap screenshot = new Bitmap(1920, 1080, PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(screenshot))
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(1920, 1080), CopyPixelOperation.SourceCopy);

        return screenshot;
    }

    static async Task SendBytesToServer(byte[] bytesImage, string serverUrl, NetworkStream stream)
    {
        try
        {
            await stream.WriteAsync(bytesImage, 0, bytesImage.Length);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending screenshot to server: {ex.Message}");
        }
    }
}

