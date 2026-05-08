using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MTASALauncher.Models
{
    public class AppSettings
    {
        public string MtaExePath { get; set; } = DetectMtaPath();
        public List<ServerEntry> FavoriteServers { get; set; } = new();

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MTASALauncher", "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
            }
            catch { }
        }

        private static string DetectMtaPath()
        {
            string[] paths =
            {
                @"C:\Program Files\MTA San Andreas 1.6\Multi Theft Auto.exe",
                @"C:\Program Files (x86)\MTA San Andreas 1.6\Multi Theft Auto.exe",
                @"C:\Program Files\MTA San Andreas 1.5\Multi Theft Auto.exe",
                @"C:\Program Files (x86)\MTA San Andreas 1.5\Multi Theft Auto.exe",
                @"C:\Program Files\MTA San Andreas\Multi Theft Auto.exe",
                @"C:\Program Files (x86)\MTA San Andreas\Multi Theft Auto.exe",
            };
            foreach (var p in paths)
                if (File.Exists(p)) return p;
            return "";
        }
    }
}
