using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;

namespace D2MultiPortConnector
{
    public class PacketPatcher
    {
        private const string D2GS_SERVER_FILE = "serverlists.txt";

        // D2GS IP -> Port mapping (from 0x08 packet or file)
        public ConcurrentDictionary<uint, ushort> D2GSPortMap { get; private set; }

        // PID -> D2CS IP:Port mapping
        public ConcurrentDictionary<int, Tuple<uint, ushort>> D2CSMap { get; private set; }

        // PID -> D2GS IP:Port mapping
        public ConcurrentDictionary<int, Tuple<uint, ushort>> D2GSMap { get; private set; }

        public event Action<string> OnLog;

        public PacketPatcher()
        {
            D2GSPortMap = new ConcurrentDictionary<uint, ushort>();
            D2CSMap = new ConcurrentDictionary<int, Tuple<uint, ushort>>();
            D2GSMap = new ConcurrentDictionary<int, Tuple<uint, ushort>>();
            LoadD2GSServersFromFile();
        }

        private void LoadD2GSServersFromFile()
        {
            /*
             * serverlists.txt format:
             * 1.2.3.4:4000
             * 1.2.3.4:4001
             * 5.6.7.8:4002
             * d2gs.example.com:4001
             */

            if (!File.Exists(D2GS_SERVER_FILE))
                return;

            try
            {
                string[] lines = File.ReadAllLines(D2GS_SERVER_FILE);

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                        continue;

                    int colonIdx = trimmed.LastIndexOf(':');
                    if (colonIdx < 0) continue;

                    string hostStr = trimmed.Substring(0, colonIdx);
                    string portStr = trimmed.Substring(colonIdx + 1);

                    ushort port;
                    if (!ushort.TryParse(portStr, out port)) continue;

                    // Handle IP or domain
                    IPAddress ipAddr;
                    if (!IPAddress.TryParse(hostStr, out ipAddr))
                    {
                        // DNS lookup for domain
                        try
                        {
                            IPAddress[] addresses = Dns.GetHostAddresses(hostStr);
                            if (addresses.Length == 0) continue;
                            ipAddr = addresses[0];
                            Log("[D2GS] DNS resolved: " + hostStr + " -> " + ipAddr);
                        }
                        catch
                        {
                            Log("[D2GS] DNS lookup failed: " + hostStr);
                            continue;
                        }
                    }

                    byte[] ipBytes = ipAddr.GetAddressBytes();
                    uint ip = BitConverter.ToUInt32(ipBytes, 0);

                    D2GSPortMap[ip] = port;
                    Log("[D2GS] Loaded: " + hostStr + " (" + ipAddr + "):" + port);
                }
            }
            catch (Exception ex)
            {
                Log("[D2GS] File load failed: " + ex.Message);
            }
        }

        public void Clear()
        {
            D2GSPortMap.Clear();
            D2CSMap.Clear();
            D2GSMap.Clear();
            LoadD2GSServersFromFile();
        }

        public Tuple<byte[], int> PatchBnetdPacket(byte[] buf, int len, int pid)
        {
            /*
             * bnetd packet:
             * +0x00: 0xFF
             * +0x01: type
             * +0x02: size (2, little endian)
             * +0x04: payload
             * 
             * SID_QUERYREALMS2 (0x40) response contains realm IP string
             * SID_LOGONREALMEX (0x3E) response may contain IP
             * 
             * SID_LOGONREALMEX (0x3E) structure:
             * +0x04: MCP Cookie (4)
             * +0x08: Status (4)
             * +0x0C: MCP Chunk 1 (4)
             * +0x10: MCP Chunk 2 (4)
             * +0x14: Realm IP (4)
             * +0x18: Realm Port (4) - only 2 bytes used
             */

            byte[] result = new byte[len];
            Array.Copy(buf, result, len);

            int offset = 0;
            while (offset + 4 <= len)
            {
                if (result[offset] != 0xFF)
                {
                    offset++;
                    continue;
                }

                byte pktType = result[offset + 1];
                ushort pktSize = BitConverter.ToUInt16(result, offset + 2);

                if (pktSize == 0 || offset + pktSize > len) break;

                // SID_LOGONREALMEX (0x3E) response
                if (pktType == 0x3E && pktSize >= 0x1C)
                {
                    uint realmIP = BitConverter.ToUInt32(result, offset + 0x14);
                    ushort realmPort = (ushort)((result[offset + 0x18] << 8) | result[offset + 0x19]);

                    Log("[BNETD] PID " + pid + " D2CS found: " + IpToString(realmIP) + ":" + realmPort);

                    // Save mapping by PID
                    D2CSMap[pid] = Tuple.Create(realmIP, realmPort);

                    // Patch to 127.0.0.1:6113
                    result[offset + 0x14] = 127;
                    result[offset + 0x15] = 0;
                    result[offset + 0x16] = 0;
                    result[offset + 0x17] = 1;

                    // 6113 in big endian
                    result[offset + 0x18] = 0x17;
                    result[offset + 0x19] = 0xE1;

                    Log("[BNETD] -> 127.0.0.1:6113 patched");
                }

                offset += pktSize;
            }

            return Tuple.Create(result, len);
        }

        public Tuple<byte[], int> PatchD2CSPacket(byte[] buf, int len, int pid)
        {
            /*
             * MCP packet (d2cs):
             * +0x00: size (2)
             * +0x02: type (1) <- 1 byte!
             * +0x03: payload
             * 
             * MCP_JOINGAME (0x04):
             * +0x03: Request ID (2)
             * +0x05: Game token (2)
             * +0x07: Unknown (2)
             * +0x09: IP of D2GS Server (4)
             * +0x0D: Game hash (4)
             * +0x11: Result (4)
             * 
             * 0x08 packet (D2GS list)
             */

            byte[] result = new byte[len];
            Array.Copy(buf, result, len);

            int offset = 0;
            while (offset + 3 <= len)
            {
                ushort pktSize = BitConverter.ToUInt16(result, offset);
                byte pktType = result[offset + 2];

                if (pktSize == 0 || offset + pktSize > len) break;

                Log("[D2CS] Packet type=0x" + pktType.ToString("X2") + " size=" + pktSize);

                // 0x08 = D2GS list
                if (pktType == 0x08 && pktSize >= 4)
                {
                    ParseD2GSList(result, offset, pktSize);
                }
                // 0x04 = MCP_JOINGAME
                else if (pktType == 0x04 && pktSize >= 0x10)
                {
                    PatchJoinGameReply(result, offset, pid);
                }

                offset += pktSize;
            }

            return Tuple.Create(result, len);
        }

        private void ParseD2GSList(byte[] buf, int offset, int pktSize)
        {
            byte count = buf[offset + 4];
            Log("[D2CS] D2GS list: " + count + " servers");

            int dataOffset = offset + 5;
            for (int i = 0; i < count; i++)
            {
                if (dataOffset + 6 > offset + pktSize) break;

                uint ip = BitConverter.ToUInt32(buf, dataOffset);
                ushort port = BitConverter.ToUInt16(buf, dataOffset + 4);

                D2GSPortMap[ip] = port;
                Log("[D2CS]   - " + IpToString(ip) + ":" + port);

                dataOffset += 6;
            }
        }

        private void PatchJoinGameReply(byte[] buf, int offset, int pid)
        {
            // +0x09: IP of D2GS Server (4)
            uint ip = BitConverter.ToUInt32(buf, offset + 0x09);

            ushort port;
            if (!D2GSPortMap.TryGetValue(ip, out port))
            {
                port = 4000;
                Log("[D2CS] Port mapping not found, default: 4000");
            }

            Log("[D2CS] PID " + pid + " joining game: " + IpToString(ip) + ":" + port);

            // Save mapping by PID
            D2GSMap[pid] = Tuple.Create(ip, port);

            // Patch to 127.0.0.1
            buf[offset + 0x09] = 127;
            buf[offset + 0x0A] = 0;
            buf[offset + 0x0B] = 0;
            buf[offset + 0x0C] = 1;

            Log("[D2CS] -> 127.0.0.1 patched");
        }

        public Tuple<uint, ushort> GetD2CSMapping(int pid)
        {
            Tuple<uint, ushort> mapping;
            if (D2CSMap.TryRemove(pid, out mapping))
                return mapping;
            return null;
        }

        public Tuple<uint, ushort> GetD2GSMapping(int pid)
        {
            Tuple<uint, ushort> mapping;
            if (D2GSMap.TryRemove(pid, out mapping))
                return mapping;
            return null;
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