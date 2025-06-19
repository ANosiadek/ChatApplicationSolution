using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatBackend
{
    public static class WebSocketHandler
    {
        private static readonly List<(WebSocket WebSocket, string? Username)> _connections = []; // Lista aktywnych połączeń WebSocket z nazwami użytkowników

        // Obsługa połączenia WebSocket, odbieranie i retransmisja wiadomości
        public static async Task HandleWebSocketAsync(WebSocket webSocket, string logFilePath)
        {
            _connections.Add((webSocket, null)); // Dodanie nowego połączenia do listy
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var message = JsonSerializer.Deserialize<ChatMessage>(messageJson);
                        if (message != null)
                        {
                            var connection = _connections.FirstOrDefault(c => c.WebSocket == webSocket);
                            _connections[_connections.IndexOf(connection)] = (webSocket, message.User); // Aktualizacja nazwy użytkownika dla połączenia

                            var logMessage = $"{DateTime.Now}: {message.User ?? "Unknown"}: {message.Content}\n";
                            await File.AppendAllTextAsync(logFilePath, logMessage); // Zapisanie wiadomości do pliku logów

                            foreach (var (ws, _) in _connections)
                            {
                                if (ws.State == WebSocketState.Open)
                                {
                                    var messageBytes = Encoding.UTF8.GetBytes(messageJson);
                                    await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None); // Retransmisja wiadomości do wszystkich klientów
                                }
                            }
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break; // Zakończenie pętli przy otrzymaniu sygnału zamknięcia
                    }
                }
            }
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(logFilePath, $"{DateTime.Now}: Error: {ex.Message}\n");
            }
            finally
            {
                var connection = _connections.FirstOrDefault(c => c.WebSocket == webSocket);
                _connections.Remove(connection); // Usunięcie połączenia z listy
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); // Zamknięcie połączenia WebSocket
            }
        }
    }

    // Szablon wiadomości chatu dla użytkownika, treści i czasu wysyłki
    public class ChatMessage
    {
        [JsonPropertyName("User")]
        public string? User { get; set; } = string.Empty;
        [JsonPropertyName("Content")]
        public string Content { get; set; } = string.Empty;
        [JsonPropertyName("Timestamp")]
        public DateTime Timestamp { get; set; }
    }
}