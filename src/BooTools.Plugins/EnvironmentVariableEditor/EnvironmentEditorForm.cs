using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BooTools.Plugins.EnvironmentVariableEditor
{
    public class EnvironmentEditorForm : Form
    {
        private readonly EnvironmentEditorPlugin _plugin;
        private readonly ListView _lvUser;
        private readonly ListView _lvSystem;
        private readonly TabControl _tabControl;
        private readonly TabPage _tabPageUser;
        private readonly TabPage _tabPageSystem;
        private readonly Button _btnAdd;
        private readonly Button _btnEdit;
        private readonly Button _btnDelete;
        private readonly Button _btnSave;
        private readonly Button _btnCancel;
        private readonly Button _btnRefresh;

        public EnvironmentEditorForm(EnvironmentEditorPlugin plugin)
        {
            _plugin = plugin;
            
            // Initialize UI Components
            _tabControl = new TabControl();
            _tabPageUser = new TabPage("用户变量");
            _lvUser = new ListView();
            _tabPageSystem = new TabPage("系统变量");
            _lvSystem = new ListView();
            _btnAdd = new Button { Text = "添加(&A)" };
            _btnEdit = new Button { Text = "编辑(&E)" };
            _btnDelete = new Button { Text = "删除(&D)" };
            _btnSave = new Button { Text = "保存(&S)" };
            _btnCancel = new Button { Text = "取消(&C)", DialogResult = DialogResult.Cancel };
            _btnRefresh = new Button { Text = "刷新(&R)" };

            InitializeComponent();
            
            // Bind Events
            _btnAdd.Click += BtnAdd_Click;
            _btnEdit.Click += BtnEdit_Click;
            _btnDelete.Click += BtnDelete_Click;
            _btnSave.Click += BtnSave_Click;
            _btnRefresh.Click += BtnRefresh_Click;
            _tabControl.SelectedIndexChanged += (s, e) => UpdateControlsState();
            this.Load += (s, e) => PopulateAllListViews();
            
            // Initial State
            UpdateControlsState();
        }

        private void InitializeComponent()
        {
            this.Text = "环境变量编辑器";
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Size = new System.Drawing.Size(800, 600);
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // TabControl
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.Controls.Add(_tabPageUser);
            _tabControl.Controls.Add(_tabPageSystem);
            
            // User Variables ListView
            SetupListView(_lvUser, "colUserName", "colUserValue");
            _tabPageUser.Controls.Add(_lvUser);

            // System Variables ListView
            SetupListView(_lvSystem, "colSystemName", "colSystemValue");
            _tabPageSystem.Controls.Add(_lvSystem);
            
            mainTable.Controls.Add(_tabControl, 0, 0);

            // Button Panel
            var buttonFlowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Padding = new Padding(0, 10, 0, 0) };
            var rightAlignedButtons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Width = 300 };

            buttonFlowPanel.Controls.Add(_btnAdd);
            buttonFlowPanel.Controls.Add(_btnEdit);
            buttonFlowPanel.Controls.Add(_btnDelete);
            buttonFlowPanel.Controls.Add(_btnRefresh);
            
            var spacer = new Panel { Dock = DockStyle.Fill };
            buttonFlowPanel.Controls.Add(spacer);
            
            rightAlignedButtons.Controls.Add(_btnCancel);
            rightAlignedButtons.Controls.Add(_btnSave);
            buttonFlowPanel.Controls.Add(rightAlignedButtons);
            
            mainTable.Controls.Add(buttonFlowPanel, 0, 1);

            this.Controls.Add(mainTable);
            this.AcceptButton = _btnSave;
            this.CancelButton = _btnCancel;
        }

        private void SetupListView(ListView listView, string col1Name, string col2Name)
        {
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.HideSelection = false;
            listView.Columns.Add(new ColumnHeader { Name = col1Name, Text = "变量名", Width = 250 });
            listView.Columns.Add(new ColumnHeader { Name = col2Name, Text = "值", Width = 450 });
        }

        private void PopulateAllListViews()
        {
            PopulateListView(_lvUser, _plugin.UserVariables);
            PopulateListView(_lvSystem, _plugin.SystemVariables);
        }

        private void PopulateListView(ListView listView, Dictionary<string, string> variables)
        {
            listView.Items.Clear();
            foreach (var variable in variables.OrderBy(v => v.Key))
            {
                var item = new ListViewItem(variable.Key);
                item.SubItems.Add(variable.Value);
                listView.Items.Add(item);
            }
        }

        private void UpdateControlsState()
        {
            bool isSystemTab = _tabControl.SelectedTab == _tabPageSystem;
            
            if (isSystemTab)
            {
                _tabPageSystem.Text = _plugin.IsAdmin ? "系统变量" : "系统变量 (只读)";
                _btnAdd.Enabled = _plugin.IsAdmin;
                _btnEdit.Enabled = _plugin.IsAdmin;
                _btnDelete.Enabled = _plugin.IsAdmin;
            }
            else
            {
                _btnAdd.Enabled = true;
                _btnEdit.Enabled = true;
                _btnDelete.Enabled = true;
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            _plugin.LoadAllVariables();
            PopulateAllListViews();
            MessageBox.Show("环境变量已刷新。", "刷新成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (_tabControl.SelectedTab == null) return;
            var activeListView = _tabControl.SelectedTab.Controls.OfType<ListView>().First();
            var activeDictionary = (_tabControl.SelectedTab == _tabPageUser) ? _plugin.UserVariables : _plugin.SystemVariables;

            using (var form = new VariableEditForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (string.IsNullOrWhiteSpace(form.VariableName))
                    {
                        MessageBox.Show("变量名不能为空。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    activeDictionary[form.VariableName] = form.VariableValue;
                    PopulateListView(activeListView, activeDictionary);
                }
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_tabControl.SelectedTab == null) return;
            var activeListView = _tabControl.SelectedTab.Controls.OfType<ListView>().First();
            if (activeListView.SelectedItems.Count == 0) return;

            var selectedItem = activeListView.SelectedItems[0];
            var varName = selectedItem.Text;
            var varValue = selectedItem.SubItems[1].Text;
            var activeDictionary = (_tabControl.SelectedTab == _tabPageUser) ? _plugin.UserVariables : _plugin.SystemVariables;

            using (var form = new VariableEditForm(varName, varValue))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (!varName.Equals(form.VariableName, StringComparison.OrdinalIgnoreCase))
                    {
                        activeDictionary.Remove(varName);
                    }
                    activeDictionary[form.VariableName] = form.VariableValue;
                    PopulateListView(activeListView, activeDictionary);
                }
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_tabControl.SelectedTab == null) return;
            var activeListView = _tabControl.SelectedTab.Controls.OfType<ListView>().First();
            if (activeListView.SelectedItems.Count == 0) return;

            var selectedItem = activeListView.SelectedItems[0];
            var varName = selectedItem.Text;
            var activeDictionary = (_tabControl.SelectedTab == _tabPageUser) ? _plugin.UserVariables : _plugin.SystemVariables;

            if (MessageBox.Show($"确定要删除变量 '{varName}' 吗?", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                activeDictionary.Remove(varName);
                PopulateListView(activeListView, activeDictionary);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            _plugin.SaveChanges();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}