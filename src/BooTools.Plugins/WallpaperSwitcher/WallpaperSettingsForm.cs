using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BooTools.Plugins.WallpaperSwitcher
{
    public partial class WallpaperSettingsForm : Form
    {
        public WallpaperConfig Config { get; private set; }
        
        public WallpaperSettingsForm(WallpaperConfig config)
        {
            Config = config ?? new WallpaperConfig();
            InitializeComponent();
            LoadSettings();
        }
        
        private void InitializeComponent()
        {
            // --- UI Standards ---
            const int standardControlHeight = 30;
            const int standardButtonWidth = 90;
            var standardMargin = new Padding(3);
            var verticalMargin = new Padding(3, 8, 3, 8);

            // --- Form Setup ---
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.Text = "壁纸切换器设置";
            this.Size = new System.Drawing.Size(600, 550);
            this.MinimumSize = new System.Drawing.Size(550, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(10);

            // --- Main Layout Panel ---
            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
            };
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Labels
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Controls
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Buttons

            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Enabled checkbox
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Interval
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Mode
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Directory
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Extensions
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // OK/Cancel Buttons

            // --- Controls Initialization ---

            // Row 1: Enabled Checkbox
            var chkEnabled = new CheckBox
            {
                Text = "启用壁纸切换",
                Checked = Config.Enabled,
                AutoSize = true,
                Height = standardControlHeight,
                Margin = new Padding(3, 6, 3, 15),
                Anchor = AnchorStyles.Left
            };
            mainTable.Controls.Add(chkEnabled, 0, 0);
            mainTable.SetColumnSpan(chkEnabled, 3);

            // Row 2: Interval
            var lblInterval = new Label { Text = "切换间隔 (秒):", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            var numInterval = new NumericUpDown { Minimum = 30, Maximum = 3600, Value = Config.Interval, Dock = DockStyle.Left, Width = 100, Height = standardControlHeight, Margin = verticalMargin };
            mainTable.Controls.Add(lblInterval, 0, 1);
            mainTable.Controls.Add(numInterval, 1, 1);

            // Row 3: Mode
            var lblMode = new Label { Text = "切换模式:", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            var cboMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 120, Height = standardControlHeight, Margin = verticalMargin };
            cboMode.Items.AddRange(new object[] { "随机", "顺序" });
            cboMode.SelectedIndex = Config.Mode == "random" ? 0 : 1;
            mainTable.Controls.Add(lblMode, 0, 2);
            mainTable.Controls.Add(cboMode, 1, 2);

            // Row 4: Directory
            var lblDirectory = new Label { Text = "壁纸目录:", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            var txtDirectory = new TextBox { Text = Config.WallpaperDirectory, Dock = DockStyle.Fill, Height = standardControlHeight, Margin = verticalMargin };
            var btnBrowse = new Button { Text = "浏览", Size = new System.Drawing.Size(standardButtonWidth, standardControlHeight), Margin = new Padding(10, 8, 3, 8) };
            mainTable.Controls.Add(lblDirectory, 0, 3);
            mainTable.Controls.Add(txtDirectory, 1, 3);
            mainTable.Controls.Add(btnBrowse, 2, 3);

            // Row 5: Extensions
            var lblExtensions = new Label { Text = "支持的文件类型:", AutoSize = true, Anchor = AnchorStyles.Top, Margin = new Padding(3, 15, 3, 0) };
            var lstExtensions = new ListBox { Dock = DockStyle.Fill, Margin = new Padding(3, 15, 3, 3), IntegralHeight = false };
            foreach (var ext in Config.FileExtensions) { lstExtensions.Items.Add(ext); }
            
            var extensionButtons = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(10, 15, 3, 3) };
            var btnAddExt = new Button { Text = "添加", Size = new System.Drawing.Size(standardButtonWidth, standardControlHeight) };
            var btnRemoveExt = new Button { Text = "删除", Size = new System.Drawing.Size(standardButtonWidth, standardControlHeight), Margin = new Padding(0, 5, 0, 0) };
            extensionButtons.Controls.Add(btnAddExt);
            extensionButtons.Controls.Add(btnRemoveExt);

            mainTable.Controls.Add(lblExtensions, 0, 4);
            mainTable.Controls.Add(lstExtensions, 1, 4);
            mainTable.Controls.Add(extensionButtons, 2, 4);

            // Row 6: OK/Cancel Buttons
            var buttonFlowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            var btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(standardButtonWidth, standardControlHeight), Margin = new Padding(10, 15, 3, 3) };
            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(standardButtonWidth, standardControlHeight), Margin = new Padding(3, 15, 3, 3) };
            buttonFlowPanel.Controls.Add(btnCancel);
            buttonFlowPanel.Controls.Add(btnOK);
            mainTable.Controls.Add(buttonFlowPanel, 0, 5);
            mainTable.SetColumnSpan(buttonFlowPanel, 3);

            // --- Event Handlers ---
            btnBrowse.Click += (s, e) => {
                using var folderDialog = new FolderBrowserDialog { Description = "选择壁纸目录", SelectedPath = txtDirectory.Text };
                if (folderDialog.ShowDialog() == DialogResult.OK) { txtDirectory.Text = folderDialog.SelectedPath; }
            };
            btnAddExt.Click += (s, e) => {
                var input = ShowInputDialog("请输入文件扩展名 (例如: .jpg)", "添加文件类型", ".jpg");
                if (!string.IsNullOrEmpty(input) && !lstExtensions.Items.Contains(input)) { lstExtensions.Items.Add(input); }
            };
            btnRemoveExt.Click += (s, e) => {
                if (lstExtensions.SelectedIndex >= 0) { lstExtensions.Items.RemoveAt(lstExtensions.SelectedIndex); }
            };
            btnOK.Click += (s, e) => {
                Config.Enabled = chkEnabled.Checked;
                Config.Interval = (int)numInterval.Value;
                Config.Mode = cboMode.SelectedIndex == 0 ? "random" : "sequential";
                Config.WallpaperDirectory = txtDirectory.Text;
                Config.FileExtensions = lstExtensions.Items.Cast<string>().ToList();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.Add(mainTable);
        }
        
        private void LoadSettings()
        {
            // 设置已在 InitializeComponent 中完成
        }
        
        private string ShowInputDialog(string prompt, string title, string defaultValue)
        {
            using var form = new Form()
            {
                Text = title,
                Size = new System.Drawing.Size(350, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            var label = new Label()
            {
                Text = prompt,
                Location = new System.Drawing.Point(12, 15),
                Size = new System.Drawing.Size(310, 20)
            };
            
            var textBox = new TextBox()
            {
                Text = defaultValue,
                Location = new System.Drawing.Point(12, 40),
                Size = new System.Drawing.Size(310, 20)
            };
            
            var btnOK = new Button()
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(170, 70),
                Size = new System.Drawing.Size(75, 23)
            };
            
            var btnCancel = new Button()
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(250, 70),
                Size = new System.Drawing.Size(75, 23)
            };
            
            form.Controls.AddRange(new Control[] { label, textBox, btnOK, btnCancel });
            form.AcceptButton = btnOK;
            form.CancelButton = btnCancel;
            
            return form.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }
    }
} 