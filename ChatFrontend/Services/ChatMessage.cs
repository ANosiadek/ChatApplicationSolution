using System.Text.Json.Serialization;

namespace ChatFrontend.Services
{
    public class ChatMessage
    {
        [JsonPropertyName("User")]
        public string User { get; set; } = string.Empty; // Nazwa użytkownika wysyłającego wiadomość
        [JsonPropertyName("Content")]
        public string Content { get; set; } = string.Empty; // Treść wiadomości
        [JsonPropertyName("Timestamp")]
        public DateTime Timestamp { get; set; } // Czas wysyłki wiadomości
    }
}