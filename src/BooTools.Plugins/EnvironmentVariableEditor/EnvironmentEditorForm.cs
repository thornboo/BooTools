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
        private readonly Dictionary<string, string> _userVariables;
        private readonly Dictionary<string, string> _systemVariables;

        public EnvironmentEditorForm()
        {
            InitializeComponent();
            _userVariables = LoadVariables(EnvironmentVariableTarget.User);
            _systemVariables = LoadVariables(EnvironmentVariableTarget.Machine);
            PopulateListView(lvUser, _userVariables);
            PopulateListView(lvSystem, _systemVariables);
            CheckAdminPrivileges();
        }

        private void InitializeComponent()
        {
            // Form setup
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Text = "Environment Variable Editor";
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            // TabControl
            this.tabControl = new TabControl();
            this.tabControl.Dock = DockStyle.Fill;
            this.tabPageUser = new TabPage("User Variables");
            this.tabPageSystem = new TabPage("System Variables");
            this.tabControl.Controls.Add(this.tabPageUser);
            this.tabControl.Controls.Add(this.tabPageSystem);

            // User Variables ListView
            this.lvUser = new ListView();
            this.lvUser.Dock = DockStyle.Fill;
            this.lvUser.View = View.Details;
            this.lvUser.FullRowSelect = true;
            this.lvUser.Columns.Add("Name", 200);
            this.lvUser.Columns.Add("Value", 400);
            this.tabPageUser.Controls.Add(this.lvUser);

            // System Variables ListView
            this.lvSystem = new ListView();
            this.lvSystem.Dock = DockStyle.Fill;
            this.lvSystem.View = View.Details;
            this.lvSystem.FullRowSelect = true;
            this.lvSystem.Columns.Add("Name", 200);
            this.lvSystem.Columns.Add("Value", 400);
            this.tabPageSystem.Controls.Add(this.lvSystem);
            
            // Buttons
            this.btnAdd = new Button { Text = "Add", Left = 10, Top = 560 };
            this.btnEdit = new Button { Text = "Edit", Left = 90, Top = 560 };
            this.btnDelete = new Button { Text = "Delete", Left = 170, Top = 560 };
            this.btnSave = new Button { Text = "Save", Left = 620, Top = 560, Anchor = (AnchorStyles.Top | AnchorStyles.Right) };
            this.btnCancel = new Button { Text = "Cancel", Left = 710, Top = 560, Anchor = (AnchorStyles.Top | AnchorStyles.Right) };

            // Add controls to form
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);

            // Button Events
            this.btnAdd.Click += BtnAdd_Click;
            this.btnEdit.Click += BtnEdit_Click;
            this.btnDelete.Click += BtnDelete_Click;
            this.btnSave.Click += BtnSave_Click;
            this.btnCancel.Click += (s, e) => this.Close();
        }

        private Dictionary<string, string> LoadVariables(EnvironmentVariableTarget target)
        {
            var variables = Environment.GetEnvironmentVariables(target);
            return variables.Cast<DictionaryEntry>().ToDictionary(de => (string)de.Key, de => (string)(de.Value ?? ""));
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

        private void CheckAdminPrivileges()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                if (!isAdmin)
                {
                    tabPageSystem.Text += " (Read-only)";
                    // Disable editing controls for system variables if not admin
                    tabControl.SelectedIndexChanged += (s, e) =>
                    {
                        bool isSystemTab = tabControl.SelectedTab == tabPageSystem;
                        btnEdit.Enabled = !isSystemTab;
                        btnDelete.Enabled = !isSystemTab;
                    };
                }
            }
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

            if (MessageBox.Show($"Are you sure you want to delete '{varName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
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
                var originalUserVars = LoadVariables(EnvironmentVariableTarget.User);
                SaveChanges(originalUserVars, _userVariables, EnvironmentVariableTarget.User);

                // Save System Variables, checking for admin rights
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        var originalSystemVars = LoadVariables(EnvironmentVariableTarget.Machine);
                        SaveChanges(originalSystemVars, _systemVariables, EnvironmentVariableTarget.Machine);
                    }
                    else
                    {
                        MessageBox.Show("Administrator privileges are required to save system variables. System variables were not saved.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                // Broadcast that environment settings have changed.
                NativeMethods.SendMessageTimeout(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE, IntPtr.Zero, "Environment", 0, 5000, out _);

                MessageBox.Show("Changes saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveChanges(Dictionary<string, string> originalVars, Dictionary<string, string> newVars, EnvironmentVariableTarget target)
        {
            // Find deleted variables
            foreach (var key in originalVars.Keys.Except(newVars.Keys))
            {
                Environment.SetEnvironmentVariable(key, null, target);
            }

            // Find added or modified variables
            foreach (var pair in newVars)
            {
                if (!originalVars.ContainsKey(pair.Key) || originalVars[pair.Key] != pair.Value)
                {
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value, target);
                }
            }
        }

        // UI component fields
        private System.ComponentModel.IContainer? components = null;
        private TabControl tabControl = null!;
        private TabPage tabPageUser = null!;
        private TabPage tabPageSystem = null!;
        private ListView lvUser = null!;
        private ListView lvSystem = null!;
        private Button btnAdd = null!;
        private Button btnEdit = null!;
        private Button btnDelete = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
    }
}
