using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MTASALauncher.Models;

namespace MTASALauncher.Services
{
    public class LauncherService
    {
        public Task LaunchMtaAsync(string exePath)
        {
            var exe = ResolveExe(exePath);
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(exe) ?? ""
            });
            return Task.CompletedTask;
        }

        public Task ConnectToServerAsync(ServerEntry server, string exePath)
        {
            var uri = $"mtasa://{server.Ip}:{server.Port}";

            // Try direct executable first; fall back to URI scheme handler
            try
            {
                var exe = ResolveExe(exePath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = uri,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(exe) ?? ""
                });
            }
            catch
            {
                // Fallback: let Windows resolve the mtasa:// URI handler
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                });
            }

            return Task.CompletedTask;
        }

        private static string ResolveExe(string configured)
        {
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
                return configured;

            string[] fallbacks =
            {
                @"C:\Program Files\MTA San Andreas 1.6\Multi Theft Auto.exe",
                @"C:\Program Files (x86)\MTA San Andreas 1.6\Multi Theft Auto.exe",
                @"C:\Program Files\MTA San Andreas 1.5\Multi Theft Auto.exe",
                @"C:\Program Files (x86)\MTA San Andreas 1.5\Multi Theft Auto.exe",
                @"C:\Program Files\MTA San Andreas\Multi Theft Auto.exe",
                @"C:\Program Files (x86)\MTA San Andreas\Multi Theft Auto.exe",
            };

            foreach (var p in fallbacks)
                if (File.Exists(p)) return p;

            throw new FileNotFoundException(
                "Az MTA:SA futtatható fájl nem található.\n" +
                "Kérlek állítsd be a helyes elérési utat a Beállítások menüben.");
        }
    }
}
