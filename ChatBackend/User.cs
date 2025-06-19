using System.Text.Json.Serialization;

namespace ChatBackend
{
    public class User
    {
        [JsonPropertyName("Username")]
        public string Username { get; set; } = string.Empty;
        [JsonPropertyName("Password")]
        public string Password { get; set; } = string.Empty;
    }
}