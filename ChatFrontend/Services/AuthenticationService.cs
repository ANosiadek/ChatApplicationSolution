using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ChatFrontend.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly User _user;
        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentUser); // Sprawdza, czy użytkownik jest zalogowany
        public string CurrentUser { get; private set; } = string.Empty; // Aktualnie zalogowany użytkownik

        public AuthenticationService(HttpClient httpClient, User user)
        {
            _httpClient = httpClient;
            _user = user;
        }

        // Walidacja poświadczeń użytkownika poprzez wysłanie żądania do serwera i zwrot statusu oraz komunikatu
        public async Task<(bool Success, string Message)> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("http://localhost:5000/login", new { Username = username, Password = password });
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                    if (result != null && !string.IsNullOrEmpty(result.Username))
                    {
                        await LoginAsync(result.Username);
                        Console.WriteLine($"Logowanie udane dla: {result.Username}");
                        return (true, "Zalogowano poprawnie");
                    }
                    return (false, "Błąd serwera");

                }
                return (false, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd logowania: {ex.Message}");
                return (false, "Błąd połączenia z serwerem");
            }
        }

        // Logowanie użytkownika poprzez ustawienie nazwy użytkownika i aktualizację stanu
        public async Task LoginAsync(string username)
        {
            CurrentUser = username ?? string.Empty;
            _user.SetUserName(username ?? string.Empty);
            await Task.CompletedTask;
        }

        // Wylogowanie użytkownika poprzez wyczyszczenie nazwy użytkownika i aktualizację stanu
        public async Task LogoutAsync()
        {
            CurrentUser = string.Empty;
            _user.SetUserName(string.Empty);
            await Task.CompletedTask;
        }

        // Szablon wyniku logowania
        public class LoginResult
        {
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
        }
    }
}