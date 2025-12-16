using System;
using System.Windows.Forms;

namespace D2MultiPortConnector
{
    public partial class D2MultiPortConnector : Form
    {
        private const int MAX_LOG_LENGTH = 75000; //75kb?
        private GatewayManager _gateway;
        private PacketPatcher _patcher;
        private ProxyServer _proxy;

        public D2MultiPortConnector()
        {
            InitializeComponent();

            _gateway = new GatewayManager();
            _patcher = new PacketPatcher();
            _proxy = new ProxyServer(_patcher);

            _patcher.OnLog += Log;
            _proxy.OnLog += Log;
        }

        private void D2MultiPortConnector_Load(object sender, EventArgs e)
        {
            string current = _gateway.GetCurrentGateway();
            if (!string.IsNullOrEmpty(current))
            {
                lblGatewayStatus.Text = "Current: " + current;
                if (current != "127.0.0.1")
                {
                    txtServerIP.Text = current;
                    btnSetGateway.Enabled = true;
                    btnRestoreGateway.Enabled = false;
                }
                else
                {
                    txtServerIP.Text = _gateway.OriginalGateway;
                    btnSetGateway.Enabled = false;
                    btnRestoreGateway.Enabled = true;
                }
            }
        }

        private void btnSetGateway_Click(object sender, EventArgs e)
        {
            if (_gateway.SetGateway("127.0.0.1"))
            {
                lblGatewayStatus.Text = "Current: 127.0.0.1 (Proxy)";
                btnSetGateway.Enabled = false;
                btnRestoreGateway.Enabled = true;
                Log("Gateway set: 127.0.0.1");
            }
            else
            {
                MessageBox.Show("Failed to set gateway");
            }
        }

        private void btnRestoreGateway_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_gateway.OriginalGateway))
            {
                MessageBox.Show("No gateway to restore.");
                return;
            }

            string originalGateway = _gateway.OriginalGateway;
            if (_gateway.RestoreGateway())
            {
                lblGatewayStatus.Text = "Current: " + originalGateway;
                btnSetGateway.Enabled = true;
                btnRestoreGateway.Enabled = false;
                Log("Gateway restored: " + originalGateway);
            }
            else
            {
                MessageBox.Show("Failed to restore gateway");
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string serverIP = txtServerIP.Text.Trim();

            if (string.IsNullOrWhiteSpace(serverIP) || serverIP == "127.0.0.1")
            {
                serverIP = _gateway.OriginalGateway;
            }

            if (string.IsNullOrWhiteSpace(serverIP) || serverIP == "127.0.0.1")
            {
                MessageBox.Show("Please enter server IP.");
                return;
            }

            try
            {
                _proxy.Start(serverIP);

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                txtServerIP.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Start failed: " + ex.Message);
                _proxy.Stop();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _proxy.Stop();

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            txtServerIP.Enabled = true;
        }

        private void Log(string msg)
        {
            if (checkBox_NoLogging.Checked) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Log(msg)));
                return;
            }

            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n");

            // 길이 초과하면 앞부분 삭제
            if (txtLog.TextLength > MAX_LOG_LENGTH)
            {
                txtLog.Text = txtLog.Text.Substring(txtLog.TextLength - MAX_LOG_LENGTH / 2);
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
        }

        private void D2MultiPortConnector_FormClosing(object sender, FormClosingEventArgs e)
        {
            _proxy.Stop();
        }
    }
}