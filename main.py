import socket
from flask import Flask, request, render_template
from flask_socketio import SocketIO, emit

app = Flask(__name__)
socketio = SocketIO(app)

click_coordinates = {"x": None, "y": None}

# Bind server socket to 0.0.0.0 to allow external connections
server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind(('0.0.0.0', 12345))  # Changed to 0.0.0.0 for external access
server_socket.listen(1)

@app.route('/upload_screenshot', methods=['POST'])
def upload_screenshot():
    screenshot_data = request.json['screenshot']
    emit('screenshot_received', {'screenshot': screenshot_data}, broadcast=True, namespace='/')
    return "Screenshot received successfully!"


@app.route('/translations')
def translations():
    return render_template('translations.html')


@socketio.on('click_coordinates', namespace='/')
def handle_click_coordinates(data):
    global click_coordinates
    x = data['x']
    y = data['y']
    print('Click Coordinates - X:', x, 'Y:', y)
    click_coordinates = {"x": x, "y": y}
    print('Click coordinates sent to client.')

    client_socket, addr = server_socket.accept()
    client_socket.sendall(f"{x},{y}".encode())
    client_socket.close()


if __name__ == '__main__':
    # Bind Flask to 0.0.0.0 to allow external connections on port 9600
    socketio.run(app, host="0.0.0.0", port=9600, debug=True, allow_unsafe_werkzeug=True, use_reloader=False)
