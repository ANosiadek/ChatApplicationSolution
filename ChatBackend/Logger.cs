using System;
using System.IO;

namespace ChatBackend
{
    public class Logger
    {
        private readonly string _logFilePath; // Ścieżka do pliku logów

        // Inicjalizacja loggera z podaniem katalogu docelowego dla pliku logów
        public Logger(string logDirectory)
        {
            _logFilePath = Path.Combine(logDirectory, "application.log");
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath) ?? throw new InvalidOperationException("Nie można utworzyć katalogu logów."));
        }

        // Zapisanie wpisu logu z określonym poziomem i wiadomością
        public void Log(string level, string message)
        {
            var logEntry = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] [{level}] {message}";
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zapisywania logu: {ex.Message}");
            }
        }

        public void Info(string message) => Log("INFO", message); // Zapisanie wiadomości na poziomie informacyjnym

        public void Warning(string message) => Log("WARNING", message); // Zapisanie wiadomości na poziomie ostrzeżenia
        public void Error(string message) => Log("ERROR", message); // Zapisanie wiadomości na poziomie błędu
    }
}