using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BooTools.Core.Interfaces;
using BooTools.Core.Models;
using BooTools.Core.PluginLoading;
using BooTools.Core.Configuration;
using BooTools.Core.Compatibility;
using BooTools.Core.Implementations;

namespace BooTools.Core.Management
{
    public class ModernPluginManager : IPluginManager, IDisposable
    {
        private readonly ILogger<ModernPluginManager> _logger;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly PluginLoader _pluginLoader;
        private readonly PluginConfigurationManager _configManager;
        private readonly ConcurrentDictionary<string, IPlugin> _plugins;
        private readonly ConcurrentDictionary<string, IPluginContext> _pluginContexts;
        private readonly string _pluginsDirectory;
        private readonly string _configDirectory;
        private readonly Version _hostVersion;
        private bool _disposed = false;
        
        public event EventHandler<PluginStatusChangedEventArgs>? PluginStatusChanged;
        
        public ModernPluginManager(
            IServiceProvider services,
            ILogger<ModernPluginManager> logger,
            IConfiguration configuration,
            string baseDirectory,
            Version hostVersion)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _hostVersion = hostVersion ?? throw new ArgumentNullException(nameof(hostVersion));
            
            _pluginsDirectory = Path.Combine(baseDirectory, "Plugins");
            _configDirectory = Path.Combine(baseDirectory, "Config", "Plugins");
            
            // Create an adapter for components that still use the old ILogger interface
            var loggerAdapter = new LoggerAdapter(_logger);

            _pluginLoader = new PluginLoader(loggerAdapter); 
            _configManager = new PluginConfigurationManager(loggerAdapter, _configDirectory);
            _plugins = new ConcurrentDictionary<string, IPlugin>();
            _pluginContexts = new ConcurrentDictionary<string, IPluginContext>();
            
            Directory.CreateDirectory(_pluginsDirectory);
            Directory.CreateDirectory(_configDirectory);
        }
        
        public async Task<PluginResult> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Modern Plugin Manager...");
                await RefreshPluginsAsync();
                _logger.LogInformation("Plugin manager initialized successfully with {PluginCount} plugins.", _plugins.Count);
                return PluginResult.Success("插件管理器初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin manager initialization failed.");
                return PluginResult.Failure($"插件管理器初始化失败: {ex.Message}", ex);
            }
        }
        
        public async Task<PluginResult<IPlugin>> LoadPluginAsync(string pluginPath, string? entryAssemblyName = null)
        {
            try
            {
                var pluginId = Path.GetFileName(pluginPath);
                if (_plugins.ContainsKey(pluginId))
                {
                    return PluginResult<IPlugin>.Failure($"Plugin already loaded: {pluginId}");
                }
                
                var loadResult = await _pluginLoader.LoadPluginAsync(pluginId, pluginPath, entryAssemblyName ?? "");
                if (!loadResult.IsSuccess || loadResult.Data == null)
                {
                    return loadResult;
                }
                
                var plugin = loadResult.Data;
                
                // Use the adapter to provide a logger to the plugin context
                var loggerAdapter = new LoggerAdapter(_logger);
                var context = new PluginContext(
                    _services, loggerAdapter, _configuration, 
                    plugin.Metadata.Id, Path.GetDirectoryName(pluginPath) ?? "", 
                    _hostVersion);
                
                var initResult = await plugin.InitializeAsync(context);
                if (!initResult.IsSuccess)
                {
                    await _pluginLoader.UnloadPluginAsync(pluginId);
                    return PluginResult<IPlugin>.Failure($"Plugin initialization failed: {initResult.ErrorMessage}");
                }
                
                plugin.StatusChanged += OnPluginStatusChanged;
                _plugins[plugin.Metadata.Id] = plugin;
                _pluginContexts[plugin.Metadata.Id] = context;
                
                _logger.LogInformation("Successfully loaded plugin: {PluginName} v{PluginVersion}", plugin.Metadata.Name, plugin.Metadata.Version);
                return PluginResult<IPlugin>.Success(plugin, "插件加载成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from path: {PluginPath}", pluginPath);
                return PluginResult<IPlugin>.Failure($"加载插件失败: {ex.Message}", ex);
            }
        }

        private async Task LoadLegacyPluginsAsync()
        {
            try
            {
                // Use a temporary old-style logger for the legacy manager, wrapped in our adapter
                var loggerAdapter = new LoggerAdapter(_logger);
                var legacyManager = new PluginManager(loggerAdapter);
                legacyManager.LoadPlugins();
                var legacyPlugins = legacyManager.GetAllPlugins();
                
                foreach (var legacyPlugin in legacyPlugins)
                {
                    try
                    {
                        var adapter = new LegacyPluginAdapter(legacyPlugin);
                        var pluginId = adapter.Metadata.Id;
                        if (_plugins.ContainsKey(pluginId)) continue;
                        
                        var context = new PluginContext(
                            _services, loggerAdapter, _configuration,
                            pluginId, _pluginsDirectory, _hostVersion);
                        
                        await adapter.InitializeAsync(context);
                        adapter.StatusChanged += OnPluginStatusChanged;
                        _plugins[pluginId] = adapter;
                        _pluginContexts[pluginId] = context;
                        
                        _logger.LogInformation("Successfully loaded legacy plugin: {LegacyPluginName}", adapter.Metadata.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load legacy plugin: {LegacyPluginName}", legacyPlugin.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load legacy plugins.");
            }
        }

        // ... (The rest of the methods: Unload, Start, Stop, etc. do not need changes as they use _logger directly)
        #region Unchanged Methods
        
        public async Task<IEnumerable<IPlugin>> GetInstalledPluginsAsync()
        {
            await Task.CompletedTask;
            return _plugins.Values.ToArray();
        }
        
        public async Task<IPlugin?> GetPluginAsync(string pluginId)
        {
            await Task.CompletedTask;
            return _plugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
        }

        public async Task<PluginResult> UnloadPluginAsync(string pluginId)
        {
            try
            {
                if (!_plugins.TryGetValue(pluginId, out var plugin))
                {
                    return PluginResult.Success($"Plugin not found or already unloaded: {pluginId}");
                }
                
                _logger.LogInformation("Unloading plugin: {PluginName}", plugin.Metadata.Name);
                
                if (plugin.Status == PluginStatus.Running)
                {
                    await plugin.StopAsync();
                }
                
                await plugin.UnloadAsync();
                plugin.StatusChanged -= OnPluginStatusChanged;
                
                _plugins.TryRemove(pluginId, out _);
                _pluginContexts.TryRemove(pluginId, out _);
                
                await _pluginLoader.UnloadPluginAsync(pluginId);
                
                _logger.LogInformation("Successfully unloaded plugin: {PluginName}", plugin.Metadata.Name);
                return PluginResult.Success("插件卸载成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload plugin: {PluginId}", pluginId);
                return PluginResult.Failure($"卸载插件失败: {ex.Message}", ex);
            }
        }
        
        public async Task<PluginResult> StartPluginAsync(string pluginId)
        {
            try
            {
                if (!_plugins.TryGetValue(pluginId, out var plugin))
                {
                    return PluginResult.Failure($"Plugin not found: {pluginId}");
                }
                
                if (plugin.Status == PluginStatus.Running)
                {
                    return PluginResult.Success($"Plugin already running: {plugin.Metadata.Name}");
                }
                
                var result = await plugin.StartAsync();
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Plugin '{PluginId}' started successfully.", pluginId);
                    await _configManager.SetPluginEnabledAsync(pluginId, true);
                }
                else
                {
                    _logger.LogWarning("Plugin '{PluginId}' failed to start. Reason: {ErrorMessage}", pluginId, result.ErrorMessage);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start plugin: {PluginId}", pluginId);
                return PluginResult.Failure($"启动插件失败: {ex.Message}", ex);
            }
        }
        
        public async Task<PluginResult> StopPluginAsync(string pluginId)
        {
            try
            {
                if (!_plugins.TryGetValue(pluginId, out var plugin))
                {
                    return PluginResult.Failure($"Plugin not found: {pluginId}");
                }
                
                if (plugin.Status != PluginStatus.Running)
                {
                    return PluginResult.Success($"Plugin not running: {plugin.Metadata.Name}");
                }
                
                var result = await plugin.StopAsync();
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Plugin '{PluginId}' stopped successfully.", pluginId);
                    await _configManager.SetPluginEnabledAsync(pluginId, false);
                }
                else
                {
                     _logger.LogWarning("Plugin '{PluginId}' failed to stop. Reason: {ErrorMessage}", pluginId, result.ErrorMessage);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop plugin: {PluginId}", pluginId);
                return PluginResult.Failure($"停止插件失败: {ex.Message}", ex);
            }
        }
        
        public async Task<PluginResult> StartAllEnabledPluginsAsync()
        {
            try
            {
                _logger.LogInformation("Starting all enabled plugins...");
                var enabledConfigs = await _configManager.GetEnabledConfigurationsAsync();
                var results = new List<string>();
                
                foreach (var config in enabledConfigs.Where(c => c.AutoStart))
                {
                    if (_plugins.ContainsKey(config.PluginId))
                    {
                        var result = await StartPluginAsync(config.PluginId);
                        results.Add($"{config.PluginId}: {(result.IsSuccess ? "Success" : result.ErrorMessage)}");
                    }
                }
                
                _logger.LogInformation("Finished starting all enabled plugins. Processed {Count} plugins.", results.Count);
                return PluginResult.Success($"批量启动完成，结果:\n{string.Join("\n", results)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start all enabled plugins.");
                return PluginResult.Failure($"启动所有启用插件失败: {ex.Message}", ex);
            }
        }
        
        public async Task<PluginResult> StopAllPluginsAsync()
        {
            try
            {
                _logger.LogInformation("Stopping all plugins...");
                var runningPlugins = _plugins.Values.Where(p => p.Status == PluginStatus.Running).ToArray();
                var results = new List<string>();
                
                foreach (var plugin in runningPlugins)
                {
                    var result = await StopPluginAsync(plugin.Metadata.Id);
                    results.Add($"{plugin.Metadata.Name}: {(result.IsSuccess ? "Success" : result.ErrorMessage)}");
                }
                
                _logger.LogInformation("Finished stopping all plugins. Processed {Count} plugins.", results.Count);
                return PluginResult.Success($"批量停止完成，结果:\n{string.Join("\n", results)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop all plugins.");
                return PluginResult.Failure($"停止所有插件失败: {ex.Message}", ex);
            }
        }
        
        public async Task<PluginResult> RefreshPluginsAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing plugin list...");
                await ScanPluginDirectoryAsync();
                await LoadLegacyPluginsAsync();
                _logger.LogInformation("Plugin list refreshed. Total plugins: {Count}", _plugins.Count);
                return PluginResult.Success($"刷新完成，当前共有 {_plugins.Count} 个插件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh plugin list.");
                return PluginResult.Failure($"刷新插件列表失败: {ex.Message}", ex);
            }
        }
        
        public async Task<PluginResult> SetPluginEnabledAsync(string pluginId, bool enabled)
        {
            try
            {
                var result = await _configManager.SetPluginEnabledAsync(pluginId, enabled);
                if (result && _plugins.TryGetValue(pluginId, out var plugin))
                {
                    if (enabled && plugin.Status != PluginStatus.Running) await StartPluginAsync(pluginId);
                    else if (!enabled && plugin.Status == PluginStatus.Running) await StopPluginAsync(pluginId);
                }
                return result ? PluginResult.Success($"Plugin state set to: {(enabled ? "Enabled" : "Disabled")}")
                              : PluginResult.Failure("Failed to set plugin state.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set plugin enabled state for {PluginId}", pluginId);
                return PluginResult.Failure($"设置插件启用状态失败: {ex.Message}", ex);
            }
        }
        
        public async Task<PluginStatistics> GetStatisticsAsync()
        {
            await Task.CompletedTask;
            var plugins = _plugins.Values.ToArray();
            return new PluginStatistics
            {
                TotalCount = plugins.Length,
                RunningCount = plugins.Count(p => p.Status == PluginStatus.Running),
                EnabledCount = plugins.Count(p => p.Status != PluginStatus.Disabled),
                ErrorCount = plugins.Count(p => p.Status == PluginStatus.Error),
                LegacyCount = plugins.Count(p => p is LegacyPluginAdapter)
            };
        }
        
        private async Task ScanPluginDirectoryAsync()
        {
            if (!Directory.Exists(_pluginsDirectory))
            {
                _logger.LogWarning("Plugins directory not found: {PluginsDirectory}", _pluginsDirectory);
                return;
            }
            
            var pluginDirs = Directory.GetDirectories(_pluginsDirectory);
            foreach (var pluginDir in pluginDirs)
            {
                try
                {
                    var pluginId = Path.GetFileName(pluginDir);
                    if (_plugins.ContainsKey(pluginId)) continue;
                    await LoadPluginAsync(pluginDir);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to scan plugin directory: {PluginDir}", pluginDir);
                }
            }
        }
        
        private void OnPluginStatusChanged(object? sender, PluginStatusChangedEventArgs e)
        {
            _logger.LogInformation("Plugin status changed: {PluginId} from {OldStatus} to {NewStatus}", e.PluginId, e.OldStatus, e.NewStatus);
            PluginStatusChanged?.Invoke(this, e);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                StopAllPluginsAsync().Wait(TimeSpan.FromSeconds(30));
                var pluginIds = _plugins.Keys.ToArray();
                foreach (var pluginId in pluginIds)
                {
                    try
                    {
                        UnloadPluginAsync(pluginId).Wait(TimeSpan.FromSeconds(10));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to dispose plugin: {PluginId}", pluginId);
                    }
                }
                _pluginLoader?.Dispose();
                _disposed = true;
            }
        }
        #endregion
    }
}
