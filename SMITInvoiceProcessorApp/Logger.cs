using System;
using System.IO;

namespace SchedulerJob
{
    public static class Logger
    {
        private static readonly string BasePath =
            AppDomain.CurrentDomain.BaseDirectory;

        private static readonly string LogFolder =
            Path.Combine(BasePath, "Logs");

        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogFolder))
                {
                    Directory.CreateDirectory(LogFolder);
                }
            }
            catch
            {
                // Never crash application for logging issue
            }
        }

        public static void Info(string message, string level = "INFO")
        {
            Write(level, message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Error(Exception ex, string message = "")
        {
            string errorMessage;

            if (string.IsNullOrWhiteSpace(message))
            {
                errorMessage =
                    $"Exception: {ex.Message}" +
                    Environment.NewLine +
                    $"StackTrace: {ex.StackTrace}";
            }
            else
            {
                errorMessage =
                    $"{message}" +
                    Environment.NewLine +
                    $"Exception: {ex.Message}" +
                    Environment.NewLine +
                    $"StackTrace: {ex.StackTrace}";
            }

            Write("ERROR", errorMessage);
        }

        private static void Write(string level, string message)
        {
            try
            {
                string fileName =
                    $"{DateTime.Now:yyyy-MM-dd}.txt";

                string fullPath =
                    Path.Combine(LogFolder, fileName);

                string logEntry =
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} " +
                    $"[{level}] " +
                    $"{message}";

                File.AppendAllText(
                    fullPath,
                    logEntry + Environment.NewLine);
            }
            catch
            {
                // logging failure must never crash app
            }
        }
    }
}