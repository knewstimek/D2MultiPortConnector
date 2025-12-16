using System;
using System.Runtime.InteropServices;

namespace D2MultiPortConnector
{
    public static class TcpHelper
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen,
            bool sort, int ipVersion, int tblClass, uint reserved);

        const int TCP_TABLE_OWNER_PID_ALL = 5;
        const int AF_INET = 2;

        [StructLayout(LayoutKind.Sequential)]
        struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public uint localPort;
            public uint remoteAddr;
            public uint remotePort;
            public uint owningPid;
        }

        public static int GetPidByConnection(int remotePort, int localPort)
        {
            /*
             * remotePort: 프록시 리슨 포트 (6112, 6113, 4000)
             * localPort: 클라이언트 로컬 포트
             * 
             * TCP 테이블에서 해당 연결의 PID 조회
             */

            int bufferSize = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);

            IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
            try
            {
                if (GetExtendedTcpTable(tcpTablePtr, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0) != 0)
                    return -1;

                int rowCount = Marshal.ReadInt32(tcpTablePtr);
                IntPtr rowPtr = tcpTablePtr + 4;

                for (int i = 0; i < rowCount; i++)
                {
                    MIB_TCPROW_OWNER_PID row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));

                    // 포트는 network byte order (big endian)
                    int rowLocalPort = ((int)(row.localPort & 0xFF) << 8) | ((int)(row.localPort >> 8) & 0xFF);
                    int rowRemotePort = ((int)(row.remotePort & 0xFF) << 8) | ((int)(row.remotePort >> 8) & 0xFF);

                    // 클라이언트 입장: localPort=클라포트, remotePort=프록시포트
                    if (rowLocalPort == localPort && rowRemotePort == remotePort)
                    {
                        return (int)row.owningPid;
                    }

                    rowPtr += Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTablePtr);
            }
            return -1;
        }
    }
}