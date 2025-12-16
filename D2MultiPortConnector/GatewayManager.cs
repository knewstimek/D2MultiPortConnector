using System;
using Microsoft.Win32;

namespace D2MultiPortConnector
{
    public class GatewayManager
    {
        private const string D2_REG_PATH = @"Software\Blizzard Entertainment\Diablo II";
        private const string GATEWAY_KEY = "BNETIP";
        private const string BACKUP_KEY = "BNETIP_BACKUP";

        public string OriginalGateway { get; private set; }

        public GatewayManager()
        {
            // 시작 시 백업된 값 로드
            LoadBackup();
        }

        private void LoadBackup()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(D2_REG_PATH))
                {
                    if (key != null)
                    {
                        OriginalGateway = key.GetValue(BACKUP_KEY) as string;
                    }
                }
            }
            catch { }
        }

        private void SaveBackup(string ip)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(D2_REG_PATH, true))
                {
                    if (key != null)
                    {
                        key.SetValue(BACKUP_KEY, ip);
                        OriginalGateway = ip;
                    }
                }
            }
            catch { }
        }

        private void ClearBackup()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(D2_REG_PATH, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(BACKUP_KEY, false);
                    }
                }
            }
            catch { }
        }

        public string GetCurrentGateway()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(D2_REG_PATH))
                {
                    if (key != null)
                    {
                        return key.GetValue(GATEWAY_KEY) as string;
                    }
                }
            }
            catch { }
            return null;
        }

        public bool SetGateway(string ip)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(D2_REG_PATH, true))
                {
                    if (key != null)
                    {
                        // 현재 값이 127.0.0.1이 아니면 백업
                        string current = key.GetValue(GATEWAY_KEY) as string;
                        if (!string.IsNullOrEmpty(current) && current != "127.0.0.1")
                        {
                            SaveBackup(current);
                        }

                        key.SetValue(GATEWAY_KEY, ip);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public bool RestoreGateway()
        {
            if (string.IsNullOrEmpty(OriginalGateway))
                return false;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(D2_REG_PATH, true))
                {
                    if (key != null)
                    {
                        key.SetValue(GATEWAY_KEY, OriginalGateway);
                        ClearBackup();
                        OriginalGateway = null;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
}