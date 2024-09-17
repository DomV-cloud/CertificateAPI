using Application.Interfaces.Logging;

namespace Infrastructure.Services.Logging
{
    public class LoggingService : ILoggingService
    {
        private readonly string _logFilePath;

        public LoggingService()
        {
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "log.txt");

            var logDirectory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public Task LogInformationAsync(string message, params object?[] args)
        {
            return LogAsync("INFO", message, args);
        }

        public Task LogWarningAsync(string message, params object?[] args)
        {
            return LogAsync("WARN", message, args);
        }

        public Task LogErrorAsync(string message, params object?[] args)
        {
            return LogAsync("ERROR", message, args);
        }

        private async Task LogAsync(string logLevel, string message, params object?[] args)
        {
            var formattedMessage = string.Format(message, args);
            var logMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formattedMessage}";
            using (var writer = new StreamWriter(_logFilePath, true))
            {
                await writer.WriteLineAsync(logMessage);
            }
        }
    }
}
