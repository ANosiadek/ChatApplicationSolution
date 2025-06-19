using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ChatBackend;

var builder = WebApplication.CreateBuilder(args); // Tworzenie konfiguratora aplikacji

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
        builder.WithOrigins("https://localhost:44351")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors();
app.UseWebSockets();

// Inicjalizacja loggera
var logDirectory = Path.Combine(app.Environment.ContentRootPath, "logs");
var logger = new Logger(logDirectory);

// Słownik do śledzenia nieudanych prób logowania
var loginAttempts = new Dictionary<string, (int Count, DateTime? LockoutUntil)>();
const int maxAttempts = 3;
const int lockoutMinutes = 5;

// Ścieżka do users.json
var usersFilePath = Path.Combine(app.Environment.ContentRootPath, "users.json");

// Buforowane opcje serializacji JSON
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

// Endpoint rejestracji
app.MapPost("/register", async (HttpContext context) =>
{
    // Sprawdzanie czy pola są puste
    logger.Info($"Próba rejestracji nowego użytkownika");
    var request = await context.Request.ReadFromJsonAsync<User>();
    if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        logger.Warning("Rejestracja nieudana: Brak nazwy użytkownika lub hasła");
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Nazwa użytkownika i hasło są wymagane");
        return;
    }

    // Otwarcie listy zarejestrowanych użytkowników
    List<User> users = [];
    if (File.Exists(usersFilePath))
    {
        var json = await File.ReadAllTextAsync(usersFilePath);
        users = JsonSerializer.Deserialize<List<User>>(json, jsonOptions) ?? [];
    }

    // Sprawdzanie czy użytkownik już istnieje przy próbie rejestracji
    if (users.Any(u => u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)))
    {
        logger.Warning($"Rejestracja nieudana: Użytkownik {request.Username} już istnieje");
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Użytkownik o tej nazwie już istnieje");
        return;
    }

    // Pomyślna rejestracja
    users.Add(new User { Username = request.Username, Password = request.Password });
    var updatedJson = JsonSerializer.Serialize(users, jsonOptions);
    await File.WriteAllTextAsync(usersFilePath, updatedJson);
    logger.Info($"Rejestracja użytkownika {request.Username} zakończona sukcesem");
    await context.Response.WriteAsync("Rejestracja zakończona sukcesem");
});

// Endpoint logowania
app.MapPost("/login", async (HttpContext context) =>
{
    // Sprawdzanie czy pola są puste
    logger.Info($"Próba logowania użytkownika");
    var request = await context.Request.ReadFromJsonAsync<User>();
    if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        logger.Warning("Logowanie nieudane: Brak nazwy użytkownika lub hasła");
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Nazwa użytkownika i hasło są wymagane");
        return;
    }

    // Sprawdzenie czy konto jest już zablokowane
    var usernameLower = request.Username.ToLower();
    if (loginAttempts.TryGetValue(usernameLower, out var attempt) && attempt.LockoutUntil.HasValue && attempt.LockoutUntil > DateTime.UtcNow)
    {
        logger.Warning($"Logowanie nieudane: Konto {request.Username} zablokowane do {attempt.LockoutUntil.Value.ToLocalTime():yyyy-MM-dd HH:mm}");
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.Response.WriteAsync($"Konto zablokowane do {attempt.LockoutUntil.Value.ToLocalTime():yyyy-MM-dd HH:mm}");
        return;
    }

    // Otwarcie listy zarejestrowanych użytkowników
    List<User> users = [];
    if (File.Exists(usersFilePath))
    {
        var json = await File.ReadAllTextAsync(usersFilePath);
        users = JsonSerializer.Deserialize<List<User>>(json, jsonOptions) ?? [];
    }

    // Mechanizm blokujący konto po 3 nieudanych próbach
    var user = users.FirstOrDefault(u => u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));
    if (user == null || user.Password != request.Password)
    {
        loginAttempts.TryGetValue(usernameLower, out attempt);
        var count = attempt.Count + 1;
        if (count >= maxAttempts)
        {
            // Przekroczenie ilości błędnych prób logowania
            loginAttempts[usernameLower] = (count, DateTime.UtcNow.AddMinutes(lockoutMinutes));
            logger.Warning($"Konto {request.Username} zablokowane przez {lockoutMinutes} minut do {DateTime.UtcNow.AddMinutes(lockoutMinutes).ToLocalTime():HH:mm}");
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync($"Konto zablokowane przez {lockoutMinutes} minut do {DateTime.UtcNow.AddMinutes(lockoutMinutes).ToLocalTime():HH:mm}");
        }
        else
        {
            // Próby błędnych logowań
            loginAttempts[usernameLower] = (count, null);
            logger.Warning($"Logowanie nieudane dla {request.Username}: Błędne dane logowania. Pozostało {maxAttempts - count} prób");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync($"Błędny login lub hasło. Pozostało prób: {maxAttempts - count}");
        }
        return;
    }

    // Reset prób logowania po sukcesie
    logger.Info($"Logowanie użytkownika {request.Username} zakończone sukcesem");
    loginAttempts.Remove(usernameLower);
    await context.Response.WriteAsJsonAsync(new { UserId = users.IndexOf(user) + 1, Username = user.Username });
});

// WebSocket
app.MapGet("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        logger.Warning("Nieprawidłowe żądanie WebSocket");
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    logger.Info("Nawiązano nowe połączenie WebSocket");
    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

    // Połączenie z plikiem logu chatu
    var logFilePath = Path.Combine(app.Environment.ContentRootPath, "logs", "chat_log.txt");
    try
    {
        await WebSocketHandler.HandleWebSocketAsync(webSocket, logFilePath);
    }
    catch (Exception ex)
    {
        logger.Error($"Błąd WebSocket: {ex.Message}");
        throw;
    }
    logger.Info("Zamknięto połączenie WebSocket");
});

app.Run();