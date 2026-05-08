using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MTASALauncher.Models
{
    public class ServerEntry : INotifyPropertyChanged
    {
        private string _name = "";
        private string _ip = "";
        private int _port = 22003;
        private int _players = -1;
        private int _maxPlayers = -1;
        private string _gameMode = "";
        private string _mapName = "";
        private bool _isOnline;
        private bool _isQuerying;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Ip
        {
            get => _ip;
            set { _ip = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        [JsonIgnore]
        public int Players
        {
            get => _players;
            set { _players = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayersDisplay)); }
        }

        [JsonIgnore]
        public int MaxPlayers
        {
            get => _maxPlayers;
            set { _maxPlayers = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayersDisplay)); }
        }

        [JsonIgnore]
        public string GameMode
        {
            get => _gameMode;
            set { _gameMode = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public string MapName
        {
            get => _mapName;
            set { _mapName = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsQuerying
        {
            get => _isQuerying;
            set { _isQuerying = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"{Ip}:{Port}" : Name;

        [JsonIgnore]
        public string PlayersDisplay => Players < 0 ? "?/?" : $"{Players}/{MaxPlayers}";

        [JsonIgnore]
        public string Address => $"{Ip}:{Port}";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
