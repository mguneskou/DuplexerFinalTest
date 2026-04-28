using System;
using System.IO;

namespace DuplexerFinalTest.Helpers
{
    public class Logger
    {
        private readonly string _logPath;

        public Logger(string logPath)
        {
            _logPath = logPath;
            Directory.CreateDirectory(logPath);
        }

        public void Log(string message)
        {
            try
            {
                string fileName = Path.Combine(_logPath, $"Log_{DateTime.Now:yyyy-MM-dd}.txt");
                File.AppendAllText(fileName, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        public void LogError(string message, Exception ex = null)
        {
            string entry = ex != null ? $"ERROR: {message} | {ex.Message}" : $"ERROR: {message}";
            Log(entry);
        }

        public string LogDirectory => _logPath;
    }
}
