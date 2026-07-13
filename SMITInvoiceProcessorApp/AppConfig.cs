using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SchedulerJob
{
    public static class AppConfig
    {
        public static string ConnectionString { get; private set; }
        public static string ScanFolder { get; private set; }
        public static string ScanFolderExFiles { get; private set; }
        public static string DropFolder { get; private set; }
        public static string PDFDropFolder { get; private set; }
        public static string SpecialPDFDropFolder { get; private set; }

        public static bool Load()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string path = Path.Combine(basePath, "settings.txt");

                if (!File.Exists(path))
                {
                    Logger.Error("settings.txt not found");
                    return false;
                }

                var settings = File.ReadAllLines(path)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Where(x => x.Contains("="))
                    .ToDictionary(
                        x => x.Substring(0, x.IndexOf("=")).Trim(),
                        x => x.Substring(x.IndexOf("=") + 1).Trim()
                    );

                ConnectionString = Get(settings, "ConnectionString");
                ScanFolder = Get(settings, "InvoiceScanFolder");
                ScanFolderExFiles = Get(settings, "InvoiceScanFolderExFiles");
                DropFolder = Get(settings, "InvoiceDropFolder");
                PDFDropFolder = Get(settings, "PDFDropFolder");
                PDFDropFolder = Get(settings, "SpecialPDFDropFolder");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Config Load Error");
                Logger.Error(ex);
                return false;
            }
        }

        private static string Get(Dictionary<string, string> settings, string key)
        {
            if (!settings.ContainsKey(key))
            {
                Logger.Error($"Missing config key: {key}");
                return "";
            }

            return settings[key];
        }
    }
}