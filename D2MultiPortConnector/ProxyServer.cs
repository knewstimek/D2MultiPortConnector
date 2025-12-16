using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace D2MultiPortConnector
{
    public class ProxyServer
    {
        private CancellationTokenSource _cts;
        private TcpListener _bnetdListener;
        private TcpListener _d2csListener;
        private TcpListener _d2gsListener;

        private string _serverIP;
        private PacketPatcher _patcher;

        public event Action<string> OnLog;

        public bool IsRunning { get; private set; }

        public ProxyServer(PacketPatcher patcher)
        {
            _patcher = patcher;
        }

        public void Start(string serverIP)
        {
            if (IsRunning) return;

            _serverIP = serverIP;
            _cts = new CancellationTokenSource();

            _bnetdListener = new TcpListener(IPAddress.Loopback, 6112);
            _d2csListener = new TcpListener(IPAddress.Loopback, 6113);
            _d2gsListener = new TcpListener(IPAddress.Loopback, 4000);

            _bnetdListener.Start();
            _d2csListener.Start();
            _d2gsListener.Start();

            Task.Run(() => AcceptBnetdLoop());
            Task.Run(() => AcceptD2CSLoop());
            Task.Run(() => AcceptD2GSLoop());

            IsRunning = true;

            Log("========================================");
            Log("Server: " + _serverIP);
            Log("[BNETD] 127.0.0.1:6112 -> " + _serverIP + ":6112");
            Log("[D2CS]  127.0.0.1:6113 -> Dynamic (PID)");
            Log("[D2GS]  127.0.0.1:4000 -> Dynamic (PID)");
            Log("========================================");
        }

        public void Stop()
        {
            if (!IsRunning) return;

            if (_cts != null)
            {
                _cts.Cancel();
            }

            try { if (_bnetdListener != null) _bnetdListener.Stop(); } catch { }
            try { if (_d2csListener != null) _d2csListener.Stop(); } catch { }
            try { if (_d2gsListener != null) _d2gsListener.Stop(); } catch { }

            _bnetdListener = null;
            _d2csListener = null;
            _d2gsListener = null;

            _patcher.Clear();

            IsRunning = false;
            Log("Proxy stopped");
        }

        private int GetClientPid(TcpClient client, int proxyPort)
        {
            try
            {
                IPEndPoint remoteEp = (IPEndPoint)client.Client.RemoteEndPoint;
                int clientPort = remoteEp.Port;
                return TcpHelper.GetPidByConnection(proxyPort, clientPort);
            }
            catch
            {
                return -1;
            }
        }

        private async Task AcceptBnetdLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _bnetdListener.AcceptTcpClientAsync();

                    int pid = GetClientPid(client, 6112);
                    Log("[BNETD] Client connected (PID: " + pid + ")");

                    Task.Run(() => HandleBnetdClient(client, pid));
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (!_cts.IsCancellationRequested)
                        Log("[BNETD] Accept error: " + ex.Message);
                }
            }
        }

        private async Task HandleBnetdClient(TcpClient client, int pid)
        {
            TcpClient server = null;

            try
            {
                server = new TcpClient();
                await server.ConnectAsync(_serverIP, 6112);

                NetworkStream cs = client.GetStream();
                NetworkStream ss = server.GetStream();

                // Client -> Server (no patch)
                Task c2s = Task.Run(async () =>
                {
                    byte[] buf = new byte[8192];
                    while (!_cts.IsCancellationRequested)
                    {
                        int n = await cs.ReadAsync(buf, 0, buf.Length);
                        if (n == 0) break;
                        await ss.WriteAsync(buf, 0, n);
                    }
                });

                // Server -> Client (patch)
                Task s2c = Task.Run(async () =>
                {
                    byte[] buf = new byte[8192];
                    while (!_cts.IsCancellationRequested)
                    {
                        int n = await ss.ReadAsync(buf, 0, buf.Length);
                        if (n == 0) break;

                        Tuple<byte[], int> result = _patcher.PatchBnetdPacket(buf, n, pid);
                        await cs.WriteAsync(result.Item1, 0, result.Item2);
                    }
                });

                await Task.WhenAny(c2s, s2c);
            }
            catch (Exception ex)
            {
                if (!_cts.IsCancellationRequested)
                    Log("[BNETD] Error: " + ex.Message);
            }
            finally
            {
                try { if (client != null) client.Close(); } catch { }
                try { if (server != null) server.Close(); } catch { }
                Log("[BNETD] Disconnected (PID: " + pid + ")");
            }
        }

        private async Task AcceptD2CSLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _d2csListener.AcceptTcpClientAsync();

                    int pid = GetClientPid(client, 6113);
                    Log("[D2CS] Client connected (PID: " + pid + ")");

                    Task.Run(() => HandleD2CSClient(client, pid));
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (!_cts.IsCancellationRequested)
                        Log("[D2CS] Accept error: " + ex.Message);
                }
            }
        }

        private async Task HandleD2CSClient(TcpClient client, int pid)
        {
            TcpClient server = null;

            try
            {
                // Get mapping by PID
                Tuple<uint, ushort> mapping = _patcher.GetD2CSMapping(pid);

                string targetIP;
                int targetPort;

                if (mapping != null)
                {
                    targetIP = IpToString(mapping.Item1);
                    targetPort = mapping.Item2;
                    Log("[D2CS] PID " + pid + " -> " + targetIP + ":" + targetPort);
                }
                else
                {
                    // No mapping, use default
                    targetIP = _serverIP;
                    targetPort = 6113;
                    Log("[D2CS] PID " + pid + " no mapping, default: " + targetIP + ":" + targetPort);
                }

                server = new TcpClient();
                await server.ConnectAsync(targetIP, targetPort);

                NetworkStream cs = client.GetStream();
                NetworkStream ss = server.GetStream();

                // Client -> Server (no patch)
                Task c2s = Task.Run(async () =>
                {
                    byte[] buf = new byte[8192];
                    while (!_cts.IsCancellationRequested)
                    {
                        int n = await cs.ReadAsync(buf, 0, buf.Length);
                        if (n == 0) break;
                        await ss.WriteAsync(buf, 0, n);
                    }
                });

                // Server -> Client (patch)
                Task s2c = Task.Run(async () =>
                {
                    byte[] buf = new byte[8192];
                    while (!_cts.IsCancellationRequested)
                    {
                        int n = await ss.ReadAsync(buf, 0, buf.Length);
                        if (n == 0) break;

                        Tuple<byte[], int> result = _patcher.PatchD2CSPacket(buf, n, pid);
                        await cs.WriteAsync(result.Item1, 0, result.Item2);
                    }
                });

                await Task.WhenAny(c2s, s2c);
            }
            catch (Exception ex)
            {
                if (!_cts.IsCancellationRequested)
                    Log("[D2CS] Error: " + ex.Message);
            }
            finally
            {
                try { if (client != null) client.Close(); } catch { }
                try { if (server != null) server.Close(); } catch { }
                Log("[D2CS] Disconnected (PID: " + pid + ")");
            }
        }

        private async Task AcceptD2GSLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _d2gsListener.AcceptTcpClientAsync();

                    int pid = GetClientPid(client, 4000);
                    Log("[D2GS] Client connected (PID: " + pid + ")");

                    Task.Run(() => HandleD2GSClient(client, pid));
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (!_cts.IsCancellationRequested)
                        Log("[D2GS] Accept error: " + ex.Message);
                }
            }
        }

        private async Task HandleD2GSClient(TcpClient client, int pid)
        {
            TcpClient server = null;

            try
            {
                // Get mapping by PID
                Tuple<uint, ushort> mapping = _patcher.GetD2GSMapping(pid);

                if (mapping == null)
                {
                    Log("[D2GS] PID " + pid + " no mapping!");
                    client.Close();
                    return;
                }

                string ipStr = IpToString(mapping.Item1);
                ushort port = mapping.Item2;
                Log("[D2GS] PID " + pid + " -> " + ipStr + ":" + port);

                server = new TcpClient();
                await server.ConnectAsync(ipStr, port);

                NetworkStream cs = client.GetStream();
                NetworkStream ss = server.GetStream();

                // Bidirectional relay (no patch)
                Task c2s = Task.Run(async () =>
                {
                    byte[] buf = new byte[8192];
                    while (!_cts.IsCancellationRequested)
                    {
                        int n = await cs.ReadAsync(buf, 0, buf.Length);
                        if (n == 0) break;
                        await ss.WriteAsync(buf, 0, n);
                    }
                });

                Task s2c = Task.Run(async () =>
                {
                    byte[] buf = new byte[8192];
                    while (!_cts.IsCancellationRequested)
                    {
                        int n = await ss.ReadAsync(buf, 0, buf.Length);
                        if (n == 0) break;
                        await cs.WriteAsync(buf, 0, n);
                    }
                });

                await Task.WhenAny(c2s, s2c);
            }
            catch (Exception ex)
            {
                if (!_cts.IsCancellationRequested)
                    Log("[D2GS] Error: " + ex.Message);
            }
            finally
            {
                try { if (client != null) client.Close(); } catch { }
                try { if (server != null) server.Close(); } catch { }
                Log("[D2GS] Disconnected (PID: " + pid + ")");
            }
        }

        private string IpToString(uint ip)
        {
            return (ip & 0xFF) + "." + ((ip >> 8) & 0xFF) + "." + ((ip >> 16) & 0xFF) + "." + ((ip >> 24) & 0xFF);
        }

        private void Log(string msg)
        {
            if (OnLog != null)
                OnLog(msg);
        }
    }
}