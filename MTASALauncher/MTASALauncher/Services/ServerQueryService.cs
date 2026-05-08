using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MTASALauncher.Models;

namespace MTASALauncher.Services
{
    public class ServerQueryService
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        public async Task QueryServerAsync(ServerEntry server)
        {
            if (server.IsQuerying) return;

            SetOnUi(server, s => s.IsQuerying = true);

            try
            {
                var result = await QueryAsync(server.Ip, server.Port);
                SetOnUi(server, s =>
                {
                    s.IsOnline = result != null;
                    s.Players = result?.Players ?? -1;
                    s.MaxPlayers = result?.MaxPlayers ?? -1;
                    if (!string.IsNullOrWhiteSpace(result?.GameMode)) s.GameMode = result.GameMode;
                    if (!string.IsNullOrWhiteSpace(result?.MapName)) s.MapName = result.MapName;
                    if (!string.IsNullOrWhiteSpace(result?.ServerName) && string.IsNullOrWhiteSpace(s.Name))
                        s.Name = result.ServerName;
                });
            }
            catch
            {
                SetOnUi(server, s => { s.IsOnline = false; s.Players = -1; s.MaxPlayers = -1; });
            }
            finally
            {
                SetOnUi(server, s => s.IsQuerying = false);
            }
        }

        private static void SetOnUi(ServerEntry server, Action<ServerEntry> action)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == false)
                Application.Current.Dispatcher.BeginInvoke(() => action(server));
            else
                action(server);
        }

        private static async Task<QueryResult?> QueryAsync(string host, int port)
        {
            try
            {
                IPAddress ip;
                if (!IPAddress.TryParse(host, out ip!))
                {
                    var addrs = await Dns.GetHostAddressesAsync(host);
                    if (addrs.Length == 0) return null;
                    ip = addrs[0];
                }

                using var udp = new UdpClient();
                udp.Client.ReceiveTimeout = (int)Timeout.TotalMilliseconds;

                var packet = BuildQueryPacket(ip, port);
                var endpoint = new IPEndPoint(ip, port);
                await udp.SendAsync(packet, packet.Length, endpoint);

                using var cts = new CancellationTokenSource(Timeout);
                var receiveTask = udp.ReceiveAsync(cts.Token).AsTask();
                var timeoutTask = Task.Delay(Timeout);

                var winner = await Task.WhenAny(receiveTask, timeoutTask);
                if (winner == timeoutTask) return null;

                return ParseResponse(receiveTask.Result.Buffer);
            }
            catch
            {
                return null;
            }
        }

        // MTA:SA UDP query packet: "MTA\0" + ip(4 bytes) + port(2 bytes LE) + 'i' (info request)
        private static byte[] BuildQueryPacket(IPAddress ip, int port)
        {
            var ipBytes = ip.MapToIPv4().GetAddressBytes();
            return new byte[]
            {
                (byte)'M', (byte)'T', (byte)'A', 0x00,
                ipBytes[0], ipBytes[1], ipBytes[2], ipBytes[3],
                (byte)(port & 0xFF), (byte)((port >> 8) & 0xFF),
                (byte)'i'
            };
        }

        // Parse MTA:SA server info response
        private static QueryResult? ParseResponse(byte[] data)
        {
            try
            {
                // Minimum header: "MTA\0" + ip(4) + port(2) + 'i' = 11 bytes
                if (data.Length < 11) return new QueryResult();
                if (data[0] != 'M' || data[1] != 'T' || data[2] != 'A') return new QueryResult();

                int pos = 11;
                var r = new QueryResult();

                // version string (1-byte length prefix)
                if (!ReadString(data, ref pos, out _)) return r;

                // http port (2 bytes LE)
                if (pos + 2 > data.Length) return r;
                pos += 2;

                // flags (1 byte)
                if (pos >= data.Length) return r;
                pos++;

                // players (2 bytes LE)
                if (pos + 2 > data.Length) return r;
                r.Players = data[pos] | (data[pos + 1] << 8);
                pos += 2;

                // max players (2 bytes LE)
                if (pos + 2 > data.Length) return r;
                r.MaxPlayers = data[pos] | (data[pos + 1] << 8);
                pos += 2;

                // server name
                if (ReadString(data, ref pos, out string? name)) r.ServerName = name;
                // game type
                if (ReadString(data, ref pos, out string? mode)) r.GameMode = mode;
                // map name
                if (ReadString(data, ref pos, out string? map)) r.MapName = map;

                return r;
            }
            catch
            {
                return new QueryResult();
            }
        }

        private static bool ReadString(byte[] data, ref int pos, out string? value)
        {
            value = null;
            if (pos >= data.Length) return false;
            int len = data[pos++];
            if (pos + len > data.Length) return false;
            value = Encoding.UTF8.GetString(data, pos, len);
            pos += len;
            return true;
        }

        private sealed class QueryResult
        {
            public int Players { get; set; } = -1;
            public int MaxPlayers { get; set; } = -1;
            public string? ServerName { get; set; }
            public string? GameMode { get; set; }
            public string? MapName { get; set; }
        }
    }
}
