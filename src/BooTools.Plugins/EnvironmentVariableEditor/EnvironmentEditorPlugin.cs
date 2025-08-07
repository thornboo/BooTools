using System;
using System.Threading.Tasks;
using BooTools.Core.Interfaces;
using BooTools.Core.Models;

namespace BooTools.Plugins.EnvironmentVariableEditor
{
    public class EnvironmentEditorPlugin : IPlugin
    {
        private PluginStatus _status = PluginStatus.Unloaded;
        private IPluginContext? _context;

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
                Version = new Version(1, 0, 0),
                Author = "thornboo"
            };
        }

        public Task<PluginResult> InitializeAsync(IPluginContext context)
        {
            try
            {
                _context = context;
                Status = PluginStatus.Initialized;
                return Task.FromResult(PluginResult.Success("Plugin initialized successfully."));
            }
            catch (Exception ex)
            {
                Status = PluginStatus.Error;
                return Task.FromResult(PluginResult.Failure($"Initialization failed: {ex.Message}", ex));
            }
        }

        public Task<PluginResult> StartAsync()
        {
            try
            {
                Status = PluginStatus.Running;
                return Task.FromResult(PluginResult.Success("Plugin started successfully."));
            }
            catch (Exception ex)
            {
                Status = PluginStatus.Error;
                return Task.FromResult(PluginResult.Failure($"Start failed: {ex.Message}", ex));
            }
        }

        public Task<PluginResult> StopAsync()
        {
            try
            {
                Status = PluginStatus.Stopped;
                return Task.FromResult(PluginResult.Success("Plugin stopped successfully."));
            }
            catch (Exception ex)
            {
                Status = PluginStatus.Error;
                return Task.FromResult(PluginResult.Failure($"Stop failed: {ex.Message}", ex));
            }
        }

        public Task<PluginResult> UnloadAsync()
        {
            try
            {
                Status = PluginStatus.Unloaded;
                _context = null;
                return Task.FromResult(PluginResult.Success("Plugin unloaded successfully."));
            }
            catch (Exception ex)
            {
                Status = PluginStatus.Error;
                return Task.FromResult(PluginResult.Failure($"Unload failed: {ex.Message}", ex));
            }
        }

        public void ShowSettings()
        {
            try
            {
                using (var editorForm = new EnvironmentEditorForm())
                {
                    editorForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                _context?.Logger?.LogError($"Error showing settings: {ex.Message}", ex);
            }
        }

        public PluginConfigurationMode GetConfigurationMode()
        {
            return PluginConfigurationMode.None;
        }

        public Task<PluginResult> ValidateDependenciesAsync(IPluginContext context)
        {
            // 这个插件没有外部依赖，总是返回成功
            return Task.FromResult(PluginResult.Success("No dependencies to validate."));
        }
    }
}
