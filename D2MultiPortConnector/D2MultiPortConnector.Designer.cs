namespace D2MultiPortConnector
{
    partial class D2MultiPortConnector
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtServerIP = new System.Windows.Forms.TextBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lblServer = new System.Windows.Forms.Label();
            this.btnSetGateway = new System.Windows.Forms.Button();
            this.btnRestoreGateway = new System.Windows.Forms.Button();
            this.lblGatewayStatus = new System.Windows.Forms.Label();
            this.checkBox_NoLogging = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // txtServerIP
            // 
            this.txtServerIP.Location = new System.Drawing.Point(84, 11);
            this.txtServerIP.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtServerIP.Name = "txtServerIP";
            this.txtServerIP.Size = new System.Drawing.Size(168, 21);
            this.txtServerIP.TabIndex = 0;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(268, 9);
            this.btnStart.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(88, 21);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(363, 9);
            this.btnStop.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(88, 21);
            this.btnStop.TabIndex = 2;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(14, 65);
            this.txtLog.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(536, 259);
            this.txtLog.TabIndex = 5;
            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(14, 14);
            this.lblServer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(60, 12);
            this.lblServer.TabIndex = 6;
            this.lblServer.Text = "Server IP:";
            // 
            // btnSetGateway
            // 
            this.btnSetGateway.Location = new System.Drawing.Point(14, 37);
            this.btnSetGateway.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnSetGateway.Name = "btnSetGateway";
            this.btnSetGateway.Size = new System.Drawing.Size(140, 21);
            this.btnSetGateway.TabIndex = 3;
            this.btnSetGateway.Text = "Set Gateway";
            this.btnSetGateway.UseVisualStyleBackColor = true;
            this.btnSetGateway.Click += new System.EventHandler(this.btnSetGateway_Click);
            // 
            // btnRestoreGateway
            // 
            this.btnRestoreGateway.Location = new System.Drawing.Point(161, 37);
            this.btnRestoreGateway.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnRestoreGateway.Name = "btnRestoreGateway";
            this.btnRestoreGateway.Size = new System.Drawing.Size(140, 21);
            this.btnRestoreGateway.TabIndex = 4;
            this.btnRestoreGateway.Text = "Restore Gateway";
            this.btnRestoreGateway.UseVisualStyleBackColor = true;
            this.btnRestoreGateway.Click += new System.EventHandler(this.btnRestoreGateway_Click);
            // 
            // lblGatewayStatus
            // 
            this.lblGatewayStatus.AutoSize = true;
            this.lblGatewayStatus.Location = new System.Drawing.Point(315, 42);
            this.lblGatewayStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblGatewayStatus.Name = "lblGatewayStatus";
            this.lblGatewayStatus.Size = new System.Drawing.Size(0, 12);
            this.lblGatewayStatus.TabIndex = 0;
            // 
            // checkBox_NoLogging
            // 
            this.checkBox_NoLogging.AutoSize = true;
            this.checkBox_NoLogging.Checked = true;
            this.checkBox_NoLogging.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox_NoLogging.Location = new System.Drawing.Point(461, 12);
            this.checkBox_NoLogging.Name = "checkBox_NoLogging";
            this.checkBox_NoLogging.Size = new System.Drawing.Size(89, 16);
            this.checkBox_NoLogging.TabIndex = 7;
            this.checkBox_NoLogging.Text = "No Logging";
            this.checkBox_NoLogging.UseVisualStyleBackColor = true;
            // 
            // D2ProxyTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(565, 333);
            this.Controls.Add(this.checkBox_NoLogging);
            this.Controls.Add(this.lblGatewayStatus);
            this.Controls.Add(this.btnRestoreGateway);
            this.Controls.Add(this.btnSetGateway);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.txtServerIP);
            this.Controls.Add(this.lblServer);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MinimumSize = new System.Drawing.Size(581, 280);
            this.Name = "D2ProxyTool";
            this.Text = "D2 Proxy Tool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.D2MultiPortConnector_FormClosing);
            this.Load += new System.EventHandler(this.D2MultiPortConnector_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.TextBox txtServerIP;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnSetGateway;
        private System.Windows.Forms.Button btnRestoreGateway;
        private System.Windows.Forms.Label lblGatewayStatus;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.CheckBox checkBox_NoLogging;
    }
}