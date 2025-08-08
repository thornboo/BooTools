using System;
using System.IO;
using System.Windows.Forms;
using BooTools.Core;

namespace BooTools.UI
{
    public partial class LogViewerForm : Form
    {
        private readonly ILogger _logger;
        private readonly TextBox _logTextBox;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private string _lastLogContent = string.Empty;

        public LogViewerForm(ILogger logger)
        {
            _logger = logger;
            
            InitializeComponent();
            
            // 创建日志显示文本框
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
            
            // 创建刷新定时器
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 每秒刷新一次
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
            
            // 初始加载日志
            RefreshLogContent();
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // LogViewerForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(1328, 919);
            Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            MinimumSize = new System.Drawing.Size(1050, 675);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "控制台";
            ResumeLayout(false);
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            RefreshLogContent();
        }

        private void RefreshLogContent()
        {
            try
            {
                // 处理 ConsoleLogger
                if (_logger is ConsoleLogger consoleLogger)
                {
                    var fileLogger = consoleLogger.GetFileLogger();
                    var logFilePath = fileLogger.GetLogFilePath();
                    if (File.Exists(logFilePath))
                    {
                        var currentContent = File.ReadAllText(logFilePath);
                        if (currentContent != _lastLogContent)
                        {
                            _logTextBox.Text = currentContent;
                            _logTextBox.SelectionStart = _logTextBox.Text.Length;
                            _logTextBox.ScrollToCaret();
                            _lastLogContent = currentContent;
                        }
                    }
                }
                // 处理 FileLogger
                else if (_logger is FileLogger fileLogger)
                {
                    var logFilePath = fileLogger.GetLogFilePath();
                    if (File.Exists(logFilePath))
                    {
                        var currentContent = File.ReadAllText(logFilePath);
                        if (currentContent != _lastLogContent)
                        {
                            _logTextBox.Text = currentContent;
                            _logTextBox.SelectionStart = _logTextBox.Text.Length;
                            _logTextBox.ScrollToCaret();
                            _lastLogContent = currentContent;
                        }
                    }
                }
                else
                {
                    _logTextBox.Text = "当前日志系统不支持文件查看";
                }
            }
            catch (Exception ex)
            {
                _logTextBox.Text = $"读取日志文件失败: {ex.Message}";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }
} 