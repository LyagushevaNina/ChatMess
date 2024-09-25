using System.Net;
using System.Net.WebSockets;
using System.Text;

#pragma warning disable CA1303

internal static class WebSocketServer
{
    // Основной метод для приема и обработки клиентов WebSocket
    private static async Task AcceptWebSocketClients(HttpListener listener)
    {
        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);

            if (context.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                WebSocket webSocket = webSocketContext.WebSocket;
                Console.WriteLine("Клиент подключен.");

                Task receiveTask = Task.Run(async () => await ReceiveMessages(webSocket).ConfigureAwait(false));  
                Task sendTask = Task.Run(async () => await SendMessages(webSocket).ConfigureAwait(false));  

                _ = await Task.WhenAny(receiveTask, sendTask).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    // Метод для отправки сообщений клиенту
    private static async Task SendMessages(WebSocket webSocket)
    {
        while (webSocket.State == WebSocketState.Open)
        {
            Console.Write("Введите сообщение для клиента: ");
            string? message = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine("Сообщение не может быть пустым.");
                continue;
            }

            byte[] responseMessage = Encoding.UTF8.GetBytes(message);

            await webSocket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine("Сообщение отправлено клиенту.");
        }
    }

    // Метод для приема сообщений от клиента
    private static async Task ReceiveMessages(WebSocket webSocket)
    {
        byte[] buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие соединения", CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine("Соединение с клиентом закрыто.");
                break;
            }

            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Сообщение от клиента: {message}");
        }
    }

    private static async Task Main()
    {
        using HttpListener httpListener = new();

        httpListener.Prefixes.Add("http://localhost:8080/ws/");

        httpListener.Start();
        Console.WriteLine("Сервер запущен и ожидает подключений...");

        await AcceptWebSocketClients(httpListener).ConfigureAwait(false);
    }
}
