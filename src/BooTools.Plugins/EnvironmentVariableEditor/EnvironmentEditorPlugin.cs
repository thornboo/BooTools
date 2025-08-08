using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using BooTools.Core.Interfaces;
using BooTools.Core.Models;
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

    public class EnvironmentEditorPlugin : IPlugin
    {
        private PluginStatus _status = PluginStatus.Unloaded;
        private IPluginContext? _context;
        private EnvironmentEditorForm? _editorForm; // 窗体实例

        public Dictionary<string, string> UserVariables { get; private set; }
        public Dictionary<string, string> SystemVariables { get; private set; }
        public bool IsAdmin { get; private set; }

        public PluginMetadata Metadata { get; private set; }

        public PluginStatus Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    var oldStatus = _status;
                    _status = value;
                    StatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(Metadata.Id, oldStatus, value));
                }
            }
        }

        public event EventHandler<PluginStatusChangedEventArgs>? StatusChanged;

        public EnvironmentEditorPlugin()
        {
            Metadata = new PluginMetadata
            {
                Id = "EnvironmentVariableEditor",
                Name = "环境变量编辑器",
                Description = "一个用于查看和编辑用户与系统环境变量的工具。",
                Version = new Version(1, 1, 0), // Incremented version
                Author = "thornboo"
            };

            UserVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SystemVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                IsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public Task<PluginResult> InitializeAsync(IPluginContext context)
        {
            _context = context;
            Status = PluginStatus.Initialized;
            LoadAllVariables();
            return Task.FromResult(PluginResult.Success("Plugin initialized successfully."));
        }

        public void LoadAllVariables()
        {
            LoadVariables(EnvironmentVariableTarget.User, UserVariables);
            LoadVariables(EnvironmentVariableTarget.Machine, SystemVariables);
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

        public void SaveChanges()
        {
            try
            {
                // Save User Variables
                UpdateEnvironmentVariables(EnvironmentVariableTarget.User, UserVariables);

                // Save System Variables, checking for admin rights again
                if (IsAdmin)
                {
                    UpdateEnvironmentVariables(EnvironmentVariableTarget.Machine, SystemVariables);
                }
                else
                {
                    MessageBox.Show("需要管理员权限才能保存系统变量。系统变量未作任何更改。", "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // Broadcast that environment settings have changed.
                NativeMethods.SendMessageTimeout(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE, IntPtr.Zero, "Environment", 0, 5000, out _);

                MessageBox.Show("更改已成功保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _context?.Logger?.LogError($"保存环境变量时发生错误: {ex.Message}", ex);
                MessageBox.Show($"保存时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateEnvironmentVariables(EnvironmentVariableTarget target, Dictionary<string, string> newVars)
        {
            var originalVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var currentVars = Environment.GetEnvironmentVariables(target);
            foreach (DictionaryEntry de in currentVars)
            {
                originalVars[(string)de.Key] = (string)(de.Value ?? "");
            }

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

        public void ShowSettings()
        {
            // The main functionality is the editor itself, so starting the plugin is the same as showing the settings.
            // This method can be kept for compatibility or for a different settings UI in the future.
            StartAsync();
        }
        
        public Task<PluginResult> StartAsync()
        {
            try
            {
                if (_editorForm == null || _editorForm.IsDisposed)
                {
                    LoadAllVariables(); // Refresh data before showing
                    _editorForm = new EnvironmentEditorForm(this);
                    _editorForm.FormClosed += (s, e) => 
                    {
                        // When the form is closed by the user, update the status
                        if (Status == PluginStatus.Running)
                        {
                            Status = PluginStatus.Stopped;
                        }
                    };
                    _editorForm.Show();
                }
                else
                {
                    _editorForm.Activate();
                }
                
                Status = PluginStatus.Running;
                return Task.FromResult(PluginResult.Success("环境变量编辑器已启动。"));
            }
            catch (Exception ex)
            {
                _context?.Logger?.LogError($"启动环境变量编辑器失败: {ex.Message}", ex);
                return Task.FromResult(PluginResult.Failure($"启动失败: {ex.Message}", ex));
            }
        }

        public Task<PluginResult> StopAsync()
        {
            try
            {
                if (_editorForm != null && !_editorForm.IsDisposed)
                {
                    _editorForm.Close();
                    _editorForm = null;
                }
                Status = PluginStatus.Stopped;
                return Task.FromResult(PluginResult.Success("环境变量编辑器已停止。"));
            }
            catch (Exception ex)
            {
                _context?.Logger?.LogError($"停止环境变量编辑器失败: {ex.Message}", ex);
                return Task.FromResult(PluginResult.Failure($"停止失败: {ex.Message}", ex));
            }
        }

        public Task<PluginResult> UnloadAsync()
        {
            // Ensure the form is closed upon unload
            if (_editorForm != null && !_editorForm.IsDisposed)
            {
                _editorForm.Close();
            }
            Status = PluginStatus.Unloaded;
            _context = null;
            return Task.FromResult(PluginResult.Success("Plugin unloaded successfully."));
        }

        public PluginConfigurationMode GetConfigurationMode()
        {
            return PluginConfigurationMode.None;
        }

        public Task<PluginResult> ValidateDependenciesAsync(IPluginContext context)
        {
            return Task.FromResult(PluginResult.Success("No dependencies to validate."));
        }
    }
}