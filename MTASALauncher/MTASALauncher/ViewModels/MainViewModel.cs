using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using MTASALauncher.Helpers;
using MTASALauncher.Models;
using MTASALauncher.Services;

namespace MTASALauncher.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly LauncherService _launcher = new();
        private readonly ServerQueryService _query = new();

        private AppSettings _settings = AppSettings.Load();
        private ServerEntry? _selectedServer;
        private string _status = "Üdvözlünk az MTA:SA Launcherben!";
        private string _activeTab = "servers";
        private string _newIpPort = "";
        private string _newName = "";
        private string _quickConnect = "";

        public ObservableCollection<ServerEntry> Servers { get; } = new();

        public ServerEntry? SelectedServer
        {
            get => _selectedServer;
            set
            {
                _selectedServer = value;
                OnPropertyChanged();
                if (value != null) _ = _query.QueryServerAsync(value);
            }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string ActiveTab
        {
            get => _activeTab;
            set { _activeTab = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsServersTab)); OnPropertyChanged(nameof(IsSettingsTab)); }
        }

        public bool IsServersTab => ActiveTab == "servers";
        public bool IsSettingsTab => ActiveTab == "settings";

        public string NewIpPort
        {
            get => _newIpPort;
            set { _newIpPort = value; OnPropertyChanged(); }
        }

        public string NewName
        {
            get => _newName;
            set { _newName = value; OnPropertyChanged(); }
        }

        public string QuickConnect
        {
            get => _quickConnect;
            set { _quickConnect = value; OnPropertyChanged(); }
        }

        public string MtaExePath
        {
            get => _settings.MtaExePath;
            set { _settings.MtaExePath = value; OnPropertyChanged(); }
        }

        public RelayCommand ShowServersCommand { get; }
        public RelayCommand ShowSettingsCommand { get; }
        public RelayCommand ConnectCommand { get; }
        public RelayCommand QuickConnectCommand { get; }
        public RelayCommand AddServerCommand { get; }
        public RelayCommand RemoveServerCommand { get; }
        public RelayCommand RefreshAllCommand { get; }
        public RelayCommand RefreshSelectedCommand { get; }
        public RelayCommand LaunchCommand { get; }
        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand BrowseExeCommand { get; }

        public MainViewModel()
        {
            ShowServersCommand = new RelayCommand(_ => ActiveTab = "servers");
            ShowSettingsCommand = new RelayCommand(_ => ActiveTab = "settings");

            ConnectCommand = new RelayCommand(
                async _ => await ConnectAsync(SelectedServer),
                _ => SelectedServer != null);

            QuickConnectCommand = new RelayCommand(
                async _ => await QuickConnectAsync(),
                _ => !string.IsNullOrWhiteSpace(QuickConnect));

            AddServerCommand = new RelayCommand(
                async _ => await AddServerAsync(),
                _ => !string.IsNullOrWhiteSpace(NewIpPort));

            RemoveServerCommand = new RelayCommand(
                _ => RemoveServer(),
                _ => SelectedServer != null);

            RefreshAllCommand = new RelayCommand(async _ => await RefreshAllAsync());
            RefreshSelectedCommand = new RelayCommand(
                async _ => await RefreshSelectedAsync(),
                _ => SelectedServer != null);

            LaunchCommand = new RelayCommand(async _ => await LaunchAsync());
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
            BrowseExeCommand = new RelayCommand(_ => BrowseExe());

            foreach (var s in _settings.FavoriteServers)
                Servers.Add(s);

            _ = RefreshAllAsync();
        }

        private async Task ConnectAsync(ServerEntry? server)
        {
            if (server == null) return;
            Status = $"Csatlakozás: {server.DisplayName}...";
            try
            {
                await _launcher.ConnectToServerAsync(server, _settings.MtaExePath);
                Status = $"MTA:SA elindítva → {server.DisplayName}";
            }
            catch (Exception ex)
            {
                Status = $"Hiba: {ex.Message}";
                MessageBox.Show(ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task QuickConnectAsync()
        {
            var entry = ParseAddress(QuickConnect, "");
            if (entry == null) { Status = "Érvénytelen cím formátum (pl: 127.0.0.1:22003)"; return; }
            await ConnectAsync(entry);
        }

        private async Task AddServerAsync()
        {
            var entry = ParseAddress(NewIpPort, NewName);
            if (entry == null) { Status = "Érvénytelen cím formátum (pl: 127.0.0.1:22003)"; return; }

            Servers.Add(entry);
            PersistServers();
            NewIpPort = "";
            NewName = "";
            Status = $"Szerver hozzáadva: {entry.DisplayName}";
            SelectedServer = entry;
            await _query.QueryServerAsync(entry);
        }

        private void RemoveServer()
        {
            if (SelectedServer == null) return;
            var removed = SelectedServer;
            SelectedServer = null;
            Servers.Remove(removed);
            PersistServers();
            Status = $"Szerver eltávolítva: {removed.DisplayName}";
        }

        private async Task RefreshAllAsync()
        {
            Status = "Szerverek frissítése...";
            await Task.WhenAll(Servers.Select(s => _query.QueryServerAsync(s)));
            Status = $"Frissítés kész — {Servers.Count(s => s.IsOnline)}/{Servers.Count} online";
        }

        private async Task RefreshSelectedAsync()
        {
            if (SelectedServer == null) return;
            Status = $"Frissítés: {SelectedServer.DisplayName}...";
            await _query.QueryServerAsync(SelectedServer);
            Status = "Kész.";
        }

        private async Task LaunchAsync()
        {
            Status = "MTA:SA indítása...";
            try
            {
                await _launcher.LaunchMtaAsync(_settings.MtaExePath);
                Status = "MTA:SA elindult.";
            }
            catch (Exception ex)
            {
                Status = $"Hiba: {ex.Message}";
                MessageBox.Show(ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings()
        {
            PersistServers();
            _settings.Save();
            Status = "Beállítások mentve.";
        }

        private void BrowseExe()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "MTA:SA futtatható fájl kiválasztása",
                Filter = "MTA:SA|Multi Theft Auto.exe|Minden fájl|*.*",
                FileName = "Multi Theft Auto.exe"
            };
            if (dlg.ShowDialog() == true)
                MtaExePath = dlg.FileName;
        }

        private void PersistServers()
        {
            _settings.FavoriteServers = Servers.ToList();
            _settings.Save();
        }

        private static ServerEntry? ParseAddress(string address, string name)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;
            var parts = address.Trim().Split(':');
            var ip = parts[0].Trim();
            if (string.IsNullOrEmpty(ip)) return null;
            int port = parts.Length > 1 && int.TryParse(parts[1], out int p) ? p : 22003;
            return new ServerEntry { Ip = ip, Port = port, Name = name.Trim() };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
