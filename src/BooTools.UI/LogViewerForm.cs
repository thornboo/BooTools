using System;
using System.Linq;
using System.Windows.Forms;

namespace BooTools.UI
{
    public partial class LogViewerForm : Form
    {
        private readonly TextBox _logTextBox;
        private readonly System.Windows.Forms.Timer _refreshTimer;

        public LogViewerForm()
        {
            InitializeComponent();
            
            _logTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9),
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.Lime
            };
            
            this.Controls.Add(_logTextBox);
            
            // Load all historical logs immediately
            LoadHistoricalLogs();

            // Set up a timer to append new logs
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 500 // Refresh every 500ms
            };
            _refreshTimer.Tick += AppendNewLogs;
            _refreshTimer.Start();
        }

        private void LoadHistoricalLogs()
        {
            // Dequeue all messages and join them. This is done once on startup.
            var allMessages = InMemoryLogger.LogMessages.ToArray();
            _logTextBox.Text = string.Join(Environment.NewLine, allMessages);
            _logTextBox.SelectionStart = _logTextBox.Text.Length;
            _logTextBox.ScrollToCaret();
        }

        private void AppendNewLogs(object? sender, EventArgs e)
        {
            // Dequeue any new messages and append them
            if (InMemoryLogger.LogMessages.TryDequeue(out var message))
            {
                var builder = new System.Text.StringBuilder();
                builder.AppendLine(message);

                while (InMemoryLogger.LogMessages.TryDequeue(out message))
                {
                    builder.AppendLine(message);
                }
                
                _logTextBox.AppendText(builder.ToString());
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            base.OnFormClosing(e);
        }
        
        // Default InitializeComponent from the designer
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // LogViewerForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(1328, 919);
            Margin = new Padding(4);
            MinimumSize = new System.Drawing.Size(1050, 675);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "实时日志控制台";
            ResumeLayout(false);
        }
    }
} 