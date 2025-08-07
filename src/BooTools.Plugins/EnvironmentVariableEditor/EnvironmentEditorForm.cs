using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace BooTools.Plugins.EnvironmentVariableEditor
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        public const uint WM_SETTINGCHANGE = 0x001A;
    }

    public partial class EnvironmentEditorForm : Form
    {
        private Dictionary<string, string> _userVariables;
        private Dictionary<string, string> _systemVariables;
        private readonly bool _isAdmin;

        public EnvironmentEditorForm()
        {
            InitializeComponent();

            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                _isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            _userVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _systemVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            LoadAndPopulateAllVariables();
            UpdateControlsState();

            // Bind events
            this.btnAdd.Click += BtnAdd_Click;
            this.btnEdit.Click += BtnEdit_Click;
            this.btnDelete.Click += BtnDelete_Click;
            this.btnRefresh.Click += BtnRefresh_Click;
            this.btnSave.Click += BtnSave_Click;
            this.btnCancel.Click += (s, e) => this.Close();
            this.tabControl.SelectedIndexChanged += (s, e) => UpdateControlsState();
        }

        private void LoadAndPopulateAllVariables()
        {
            LoadVariables(EnvironmentVariableTarget.User, _userVariables);
            LoadVariables(EnvironmentVariableTarget.Machine, _systemVariables);
            PopulateListView(lvUser, _userVariables);
            PopulateListView(lvSystem, _systemVariables);
        }

        private void LoadVariables(EnvironmentVariableTarget target, Dictionary<string, string> dictionary)
        {
            dictionary.Clear();
            var variables = Environment.GetEnvironmentVariables(target);
            foreach (DictionaryEntry de in variables)
            {
                dictionary[(string)de.Key] = (string)(de.Value ?? "");
            }
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
            bool isSystemTab = tabControl.SelectedTab == tabPageSystem;
            
            if (isSystemTab)
            {
                tabPageSystem.Text = _isAdmin ? "系统变量" : "系统变量 (只读)";
                btnAdd.Enabled = _isAdmin;
                btnEdit.Enabled = _isAdmin;
                btnDelete.Enabled = _isAdmin;
            }
            else
            {
                btnAdd.Enabled = true;
                btnEdit.Enabled = true;
                btnDelete.Enabled = true;
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadAndPopulateAllVariables();
            MessageBox.Show("环境变量已刷新。", "刷新成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            var selectedTab = tabControl.SelectedTab;
            if (selectedTab == null) return;
            
            var activeListView = selectedTab.Controls.OfType<ListView>().First();
            var activeDictionary = (selectedTab == tabPageUser) ? _userVariables : _systemVariables;

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
            var selectedTab = tabControl.SelectedTab;
            if (selectedTab == null) return;
            
            var activeListView = selectedTab.Controls.OfType<ListView>().First();
            if (activeListView.SelectedItems.Count == 0) return;

            var selectedItem = activeListView.SelectedItems[0];
            var varName = selectedItem.Text;
            var varValue = selectedItem.SubItems[1].Text;
            var activeDictionary = (selectedTab == tabPageUser) ? _userVariables : _systemVariables;

            using (var form = new VariableEditForm(varName, varValue))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // If name changed, remove old one
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
            var selectedTab = tabControl.SelectedTab;
            if (selectedTab == null) return;
            
            var activeListView = selectedTab.Controls.OfType<ListView>().First();
            if (activeListView.SelectedItems.Count == 0) return;

            var selectedItem = activeListView.SelectedItems[0];
            var varName = selectedItem.Text;
            var activeDictionary = (selectedTab == tabPageUser) ? _userVariables : _systemVariables;

            if (MessageBox.Show($"确定要删除变量 '{varName}' 吗?", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                activeDictionary.Remove(varName);
                PopulateListView(activeListView, activeDictionary);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                // Save User Variables
                SaveChanges(EnvironmentVariableTarget.User, _userVariables);

                // Save System Variables, checking for admin rights again
                if (_isAdmin)
                {
                    SaveChanges(EnvironmentVariableTarget.Machine, _systemVariables);
                }
                else
                {
                    MessageBox.Show("需要管理员权限才能保存系统变量。系统变量未作任何更改。", "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // Broadcast that environment settings have changed.
                NativeMethods.SendMessageTimeout(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE, IntPtr.Zero, "Environment", 0, 5000, out _);

                MessageBox.Show("更改已成功保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveChanges(EnvironmentVariableTarget target, Dictionary<string, string> newVars)
        {
            var originalVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            LoadVariables(target, originalVars);

            // Find and apply deleted variables
            foreach (var key in originalVars.Keys.Except(newVars.Keys, StringComparer.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(key, null, target);
            }

            // Find and apply added or modified variables
            foreach (var pair in newVars)
            {
                if (!originalVars.ContainsKey(pair.Key) || !originalVars[pair.Key].Equals(pair.Value, StringComparison.Ordinal))
                {
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value, target);
                }
            }
        }
    }
}
