using System.Text.Json.Serialization;

namespace ChatFrontend.Services
{
    public class User
    {
        [JsonPropertyName("Username")]
        public string Username { get; private set; } = string.Empty; // Nazwa użytkownika do odczytu

        // Ustawienie nowej nazwy użytkownika i wyświetlenie informacji w konsoli
        public void SetUserName(string username)
        {
            Username = username;
            Console.WriteLine($"Zmiana użytkownika na {Username}");
        }

        // Sprawdzanie, czy użytkownik jest autoryzowany, i zwrócenie stanu autoryzacji
        public bool CheckUserName()
        {
            bool isAuthorized = !string.IsNullOrEmpty(Username);
            Console.WriteLine(isAuthorized
                ? $"Autoryzowano dla {Username}"
                : $"Brak autoryzacji dostępu.");
            return isAuthorized;
        }
    }
}