using System;
using System.Windows.Forms;
using BooTools.Core;

namespace BooTools.UI
{
    public partial class DebugConfigForm : Form
    {
        private readonly ILogger _logger;
        private ComboBox _logLevelComboBox = null!;
        private CheckBox _showConsoleCheckBox = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public DebugConfigForm(ILogger logger)
        {
            _logger = logger;
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            // --- UI Standards ---
            const int standardControlHeight = 30;
            const int standardButtonWidth = 90;
            var verticalMargin = new Padding(3, 8, 3, 8);

            // --- Form Setup ---
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.Text = "调试配置";
            this.Size = new System.Drawing.Size(500, 400);
            this.MinimumSize = new System.Drawing.Size(450, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(10);

            // --- Main Layout Panel ---
            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
            };
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Labels
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Controls
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Log Level
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Console Checkbox
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Description
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            // --- Controls Initialization ---

            // Row 1: Log Level
            var lblLogLevel = new Label
            {
                Text = "日志级别:",
                Anchor = AnchorStyles.Left,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                AutoSize = true,
                Margin = new Padding(3, 0, 10, 0)
            };

            _logLevelComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = standardControlHeight,
                Margin = verticalMargin
            };
            _logLevelComboBox.Items.AddRange(new object[]
            {
                "DEBUG - 调试信息",
                "INFO - 普通信息",
                "WARNING - 警告信息",
                "ERROR - 错误信息"
            });

            // Row 2: Console Checkbox
            var lblConsole = new Label
            {
                Text = "控制台输出:",
                Anchor = AnchorStyles.Left,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                AutoSize = true,
                Margin = new Padding(3, 0, 10, 0)
            };

            _showConsoleCheckBox = new CheckBox
            {
                Text = "启用控制台输出",
                Dock = DockStyle.Left,
                AutoSize = true,
                Height = standardControlHeight,
                Margin = verticalMargin
            };

            // Row 3: Description
            var lblDescription = new Label
            {
                Text = "日志级别说明:\n" +
                        "• DEBUG: 显示所有日志信息，包括调试信息\n" +
                        "• INFO: 显示普通信息、警告和错误\n" +
                        "• WARNING: 只显示警告和错误信息\n" +
                        "• ERROR: 只显示错误信息",
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.Fixed3D,
                Padding = new Padding(5),
                Margin = new Padding(3, 10, 3, 10)
            };

            // Row 4: Buttons
            var buttonFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };

            _btnSave = new Button
            {
                Text = "保存",
                DialogResult = DialogResult.OK,
                Size = new System.Drawing.Size(standardButtonWidth, standardControlHeight),
                Margin = new Padding(10, 5, 3, 5)
            };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Size = new System.Drawing.Size(standardButtonWidth, standardControlHeight),
                Margin = new Padding(3, 5, 3, 5)
            };

            buttonFlowPanel.Controls.Add(_btnCancel);
            buttonFlowPanel.Controls.Add(_btnSave);

            // --- Add controls to TableLayoutPanel ---
            mainTable.Controls.Add(lblLogLevel, 0, 0);
            mainTable.Controls.Add(_logLevelComboBox, 1, 0);
            mainTable.Controls.Add(lblConsole, 0, 1);
            mainTable.Controls.Add(_showConsoleCheckBox, 1, 1);
            mainTable.Controls.Add(lblDescription, 0, 2);
            mainTable.SetColumnSpan(lblDescription, 2);
            mainTable.Controls.Add(buttonFlowPanel, 0, 3);
            mainTable.SetColumnSpan(buttonFlowPanel, 2);

            // --- Finalize Form ---
            this.Controls.Add(mainTable);
            this.AcceptButton = _btnSave;
            this.CancelButton = _btnCancel;
        }

        private void LoadCurrentSettings()
        {
            // 加载当前日志级别
            if (_logger is ConsoleLogger consoleLogger)
            {
                var currentLevel = consoleLogger.GetLogLevel();
                switch (currentLevel)
                {
                    case FileLogger.LogLevel.DEBUG:
                        _logLevelComboBox.SelectedIndex = 0;
                        break;
                    case FileLogger.LogLevel.INFO:
                        _logLevelComboBox.SelectedIndex = 1;
                        break;
                    case FileLogger.LogLevel.WARNING:
                        _logLevelComboBox.SelectedIndex = 2;
                        break;
                    case FileLogger.LogLevel.ERROR:
                        _logLevelComboBox.SelectedIndex = 3;
                        break;
                }
            }
            else if (_logger is FileLogger fileLogger)
            {
                var currentLevel = fileLogger.GetLogLevel();
                switch (currentLevel)
                {
                    case FileLogger.LogLevel.DEBUG:
                        _logLevelComboBox.SelectedIndex = 0;
                        break;
                    case FileLogger.LogLevel.INFO:
                        _logLevelComboBox.SelectedIndex = 1;
                        break;
                    case FileLogger.LogLevel.WARNING:
                        _logLevelComboBox.SelectedIndex = 2;
                        break;
                    case FileLogger.LogLevel.ERROR:
                        _logLevelComboBox.SelectedIndex = 3;
                        break;
                }
            }

            // 默认选中第一项
            if (_logLevelComboBox.SelectedIndex == -1)
            {
                _logLevelComboBox.SelectedIndex = 1; // INFO
            }

            // 控制台输出默认启用
            _showConsoleCheckBox.Checked = true;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                // 获取选择的日志级别
                FileLogger.LogLevel selectedLevel = FileLogger.LogLevel.INFO;
                switch (_logLevelComboBox.SelectedIndex)
                {
                    case 0:
                        selectedLevel = FileLogger.LogLevel.DEBUG;
                        break;
                    case 1:
                        selectedLevel = FileLogger.LogLevel.INFO;
                        break;
                    case 2:
                        selectedLevel = FileLogger.LogLevel.WARNING;
                        break;
                    case 3:
                        selectedLevel = FileLogger.LogLevel.ERROR;
                        break;
                }

                // 应用设置
                if (_logger is ConsoleLogger consoleLogger)
                {
                    consoleLogger.SetLogLevel(selectedLevel);
                }
                else if (_logger is FileLogger fileLogger)
                {
                    fileLogger.SetLogLevel(selectedLevel);
                }

                MessageBox.Show("调试配置已保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 