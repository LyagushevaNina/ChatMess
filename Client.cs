using System.Net.WebSockets;
using System.Text;

#pragma warning disable CA1303

internal static class WebSocketClient
{
    private static async Task Main()
    {

        using ClientWebSocket webSocket = new();

        Uri serverUri = new("ws://localhost:8080/ws/");

        await webSocket.ConnectAsync(serverUri, CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine("Подключение к серверу установлено.");

        Task sendTask = Task.Run(async () => await SendMessages(webSocket).ConfigureAwait(false));  
        Task receiveTask = Task.Run(async () => await ReceiveMessages(webSocket).ConfigureAwait(false));  

        _ = await Task.WhenAny(sendTask, receiveTask).ConfigureAwait(false);

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Завершение работы клиента", CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine("Соединение закрыто.");
    }

    // Метод для отправки сообщений на сервер
    private static async Task SendMessages(ClientWebSocket webSocket)
    {

        while (webSocket.State == WebSocketState.Open)
        {

            Console.Write("Введите сообщение: ");
            string? message = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine("Сообщение не может быть пустым.");
                continue;
            }

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine("Сообщение отправлено серверу.");
        }
    }

    // Метод для приема сообщений от сервера
    private static async Task ReceiveMessages(ClientWebSocket webSocket)
    {

        byte[] buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {

            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("Сервер закрыл соединение.");
                break;
            }

            string serverMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Ответ от сервера: {serverMessage}");
        }
    }
}
