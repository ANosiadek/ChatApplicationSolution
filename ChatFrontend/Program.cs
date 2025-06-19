using ChatFrontend.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ChatFrontend.Services;

namespace ChatFrontend
{
    public class Program
    {
        // Główna metoda uruchamiająca aplikację
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args); // Tworzenie konfiguratora aplikacji

            // Dodawanie usług do kontenera wstrzykiwania zależności
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents(); // Rejestracja komponentów Razor z trybem interaktywnym serwer

            builder.Services.AddHttpClient(); // Rejestracja klienta HTTP do komunikacji z serwerem
            builder.Services.AddScoped<Services.AuthenticationService>(); // Rejestracja usługi autoryzacji jako singleton
            builder.Services.AddScoped<User>(); // Rejestracja usługi użytkownika jako singleton
            
            var app = builder.Build();

            // Konfiguracja potoku żądań HTTP
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true); // Ustawienie obsługi błędów z tworzeniem zakresu dla błędów
                app.UseHsts(); // Włączenie nagłówka HSTS dla bezpieczeństwa
            }

            app.UseHttpsRedirection(); // Przekierowanie na HTTPS
            app.UseStaticFiles(); // Włączenie obsługi plików statycznych
            app.UseAntiforgery(); // Włączenie ochrony przed fałszerstwem żądań

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode(); // Ustawienie trybu renderowania interaktywnego serwera

            app.Run();
        }
    }
}
