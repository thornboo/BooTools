using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using BooTools.Core.Interfaces;
using BooTools.Core.Models;
using BooTools.Core.PluginLoading;
using BooTools.Core.Configuration;
using BooTools.Core.Compatibility;
using BooTools.Core.Implementations;

namespace BooTools.Core.Management
{
    /// <summary>
    /// 现代插件管理器实现
    /// </summary>
    public class ModernPluginManager : IPluginManager
    {
        private readonly BooTools.Core.ILogger _logger;
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
        
        /// <summary>
        /// 插件状态变化事件
        /// </summary>
        public event EventHandler<PluginStatusChangedEventArgs>? PluginStatusChanged;
        
        /// <summary>
        /// 初始化现代插件管理器
        /// </summary>
        /// <param name="services">服务提供者</param>
        /// <param name="logger">日志服务</param>
        /// <param name="configuration">配置服务</param>
        /// <param name="baseDirectory">基础目录</param>
        /// <param name="hostVersion">主机版本</param>
        public ModernPluginManager(
            IServiceProvider services,
            BooTools.Core.ILogger logger,
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
            
            _pluginLoader = new PluginLoader(_logger);
            _configManager = new PluginConfigurationManager(_logger, _configDirectory);
            _plugins = new ConcurrentDictionary<string, IPlugin>();
            _pluginContexts = new ConcurrentDictionary<string, IPluginContext>();
            
            // 确保目录存在
            Directory.CreateDirectory(_pluginsDirectory);
            Directory.CreateDirectory(_configDirectory);
        }
        
        /// <summary>
        /// 初始化插件管理器
        /// </summary>
        /// <returns>初始化结果</returns>
        public async Task<PluginResult> InitializeAsync()
        {
            try
            {
                _logger.LogInfo("初始化现代插件管理器...");
                
                // 加载所有插件
                await RefreshPluginsAsync();
                
                _logger.LogInfo($"插件管理器初始化完成，共加载 {_plugins.Count} 个插件");
                return PluginResult.Success("插件管理器初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("插件管理器初始化失败", ex);
                return PluginResult.Failure($"插件管理器初始化失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取所有已安装的插件
        /// </summary>
        /// <returns>插件列表</returns>
        public async Task<IEnumerable<IPlugin>> GetInstalledPluginsAsync()
        {
            await Task.CompletedTask;
            return _plugins.Values.ToArray();
        }
        
        /// <summary>
        /// 根据ID获取插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>插件实例</returns>
        public async Task<IPlugin?> GetPluginAsync(string pluginId)
        {
            await Task.CompletedTask;
            return _plugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
        }
        
        /// <summary>
        /// 加载插件
        /// </summary>
        /// <param name="pluginPath">插件路径</param>
        /// <param name="entryAssemblyName">入口程序集名称</param>
        /// <returns>加载结果</returns>
        public async Task<PluginResult<IPlugin>> LoadPluginAsync(string pluginPath, string? entryAssemblyName = null)
        {
            try
            {
                // 生成插件ID
                var pluginId = Path.GetFileName(pluginPath);
                
                // 检查是否已加载
                if (_plugins.ContainsKey(pluginId))
                {
                    return PluginResult<IPlugin>.Failure($"插件已加载: {pluginId}");
                }
                
                // 使用插件加载器加载插件
                var loadResult = await _pluginLoader.LoadPluginAsync(pluginId, pluginPath, entryAssemblyName ?? "");
                if (!loadResult.IsSuccess || loadResult.Data == null)
                {
                    return loadResult;
                }
                
                var plugin = loadResult.Data;
                
                // 创建插件上下文
                var context = new PluginContext(
                    _services, _logger, _configuration, 
                    plugin.Metadata.Id, Path.GetDirectoryName(pluginPath) ?? "", 
                    _hostVersion);
                
                // 初始化插件
                var initResult = await plugin.InitializeAsync(context);
                if (!initResult.IsSuccess)
                {
                    await _pluginLoader.UnloadPluginAsync(pluginId);
                    return PluginResult<IPlugin>.Failure($"插件初始化失败: {initResult.ErrorMessage}");
                }
                
                // 订阅插件状态变化事件
                plugin.StatusChanged += OnPluginStatusChanged;
                
                // 注册插件
                _plugins[plugin.Metadata.Id] = plugin;
                _pluginContexts[plugin.Metadata.Id] = context;
                
                _logger.LogInfo($"成功加载插件: {plugin.Metadata.Name} v{plugin.Metadata.Version}");
                return PluginResult<IPlugin>.Success(plugin, "插件加载成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"加载插件失败: {pluginPath}", ex);
                return PluginResult<IPlugin>.Failure($"加载插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 卸载插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>卸载结果</returns>
        public async Task<PluginResult> UnloadPluginAsync(string pluginId)
        {
            try
            {
                if (!_plugins.TryGetValue(pluginId, out var plugin))
                {
                    return PluginResult.Success($"插件未找到或已卸载: {pluginId}");
                }
                
                _logger.LogInfo($"开始卸载插件: {plugin.Metadata.Name}");
                
                // 停止插件
                if (plugin.Status == PluginStatus.Running)
                {
                    await plugin.StopAsync();
                }
                
                // 卸载插件
                await plugin.UnloadAsync();
                
                // 取消订阅事件
                plugin.StatusChanged -= OnPluginStatusChanged;
                
                // 从集合中移除
                _plugins.TryRemove(pluginId, out _);
                _pluginContexts.TryRemove(pluginId, out _);
                
                // 卸载插件程序集
                await _pluginLoader.UnloadPluginAsync(pluginId);
                
                _logger.LogInfo($"成功卸载插件: {plugin.Metadata.Name}");
                return PluginResult.Success("插件卸载成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"卸载插件失败: {pluginId}", ex);
                return PluginResult.Failure($"卸载插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 启动插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>启动结果</returns>
        public async Task<PluginResult> StartPluginAsync(string pluginId)
        {
            try
            {
                if (!_plugins.TryGetValue(pluginId, out var plugin))
                {
                    return PluginResult.Failure($"插件未找到: {pluginId}");
                }
                
                if (plugin.Status == PluginStatus.Running)
                {
                    return PluginResult.Success($"插件已在运行: {plugin.Metadata.Name}");
                }
                
                var result = await plugin.StartAsync();
                if (result.IsSuccess)
                {
                    // 更新配置中的启用状态
                    await _configManager.SetPluginEnabledAsync(pluginId, true);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"启动插件失败: {pluginId}", ex);
                return PluginResult.Failure($"启动插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 停止插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>停止结果</returns>
        public async Task<PluginResult> StopPluginAsync(string pluginId)
        {
            try
            {
                if (!_plugins.TryGetValue(pluginId, out var plugin))
                {
                    return PluginResult.Failure($"插件未找到: {pluginId}");
                }
                
                if (plugin.Status != PluginStatus.Running)
                {
                    return PluginResult.Success($"插件未在运行: {plugin.Metadata.Name}");
                }
                
                var result = await plugin.StopAsync();
                if (result.IsSuccess)
                {
                    // 更新配置中的启用状态
                    await _configManager.SetPluginEnabledAsync(pluginId, false);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"停止插件失败: {pluginId}", ex);
                return PluginResult.Failure($"停止插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 启动所有启用的插件
        /// </summary>
        /// <returns>启动结果</returns>
        public async Task<PluginResult> StartAllEnabledPluginsAsync()
        {
            try
            {
                _logger.LogInfo("开始启动所有启用的插件...");
                
                var enabledConfigs = await _configManager.GetEnabledConfigurationsAsync();
                var results = new List<string>();
                
                foreach (var config in enabledConfigs.Where(c => c.AutoStart))
                {
                    if (_plugins.ContainsKey(config.PluginId))
                    {
                        var result = await StartPluginAsync(config.PluginId);
                        results.Add($"{config.PluginId}: {(result.IsSuccess ? "成功" : result.ErrorMessage)}");
                    }
                }
                
                _logger.LogInfo($"启动所有启用插件完成，处理了 {results.Count} 个插件");
                return PluginResult.Success($"批量启动完成，结果:\n{string.Join("\n", results)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("启动所有启用插件失败", ex);
                return PluginResult.Failure($"启动所有启用插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 停止所有插件
        /// </summary>
        /// <returns>停止结果</returns>
        public async Task<PluginResult> StopAllPluginsAsync()
        {
            try
            {
                _logger.LogInfo("开始停止所有插件...");
                
                var runningPlugins = _plugins.Values.Where(p => p.Status == PluginStatus.Running).ToArray();
                var results = new List<string>();
                
                foreach (var plugin in runningPlugins)
                {
                    var result = await StopPluginAsync(plugin.Metadata.Id);
                    results.Add($"{plugin.Metadata.Name}: {(result.IsSuccess ? "成功" : result.ErrorMessage)}");
                }
                
                _logger.LogInfo($"停止所有插件完成，处理了 {results.Count} 个插件");
                return PluginResult.Success($"批量停止完成，结果:\n{string.Join("\n", results)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("停止所有插件失败", ex);
                return PluginResult.Failure($"停止所有插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 刷新插件列表
        /// </summary>
        /// <returns>刷新结果</returns>
        public async Task<PluginResult> RefreshPluginsAsync()
        {
            try
            {
                _logger.LogInfo("开始刷新插件列表...");
                
                // 扫描插件目录
                await ScanPluginDirectoryAsync();
                
                // 加载旧版插件（向后兼容）
                await LoadLegacyPluginsAsync();
                
                _logger.LogInfo($"插件列表刷新完成，当前共有 {_plugins.Count} 个插件");
                return PluginResult.Success($"刷新完成，当前共有 {_plugins.Count} 个插件");
            }
            catch (Exception ex)
            {
                _logger.LogError("刷新插件列表失败", ex);
                return PluginResult.Failure($"刷新插件列表失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 设置插件启用状态
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="enabled">是否启用</param>
        /// <returns>设置结果</returns>
        public async Task<PluginResult> SetPluginEnabledAsync(string pluginId, bool enabled)
        {
            try
            {
                var result = await _configManager.SetPluginEnabledAsync(pluginId, enabled);
                
                if (result && _plugins.TryGetValue(pluginId, out var plugin))
                {
                    if (enabled && plugin.Status != PluginStatus.Running)
                    {
                        await StartPluginAsync(pluginId);
                    }
                    else if (!enabled && plugin.Status == PluginStatus.Running)
                    {
                        await StopPluginAsync(pluginId);
                    }
                }
                
                return result 
                    ? PluginResult.Success($"插件状态设置成功: {(enabled ? "启用" : "禁用")}")
                    : PluginResult.Failure("插件状态设置失败");
            }
            catch (Exception ex)
            {
                _logger.LogError($"设置插件启用状态失败: {pluginId}", ex);
                return PluginResult.Failure($"设置插件启用状态失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取插件统计信息
        /// </summary>
        /// <returns>统计信息</returns>
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
        
        /// <summary>
        /// 扫描插件目录
        /// </summary>
        private async Task ScanPluginDirectoryAsync()
        {
            if (!Directory.Exists(_pluginsDirectory))
            {
                _logger.LogWarning($"插件目录不存在: {_pluginsDirectory}");
                return;
            }
            
            var pluginDirs = Directory.GetDirectories(_pluginsDirectory);
            
            foreach (var pluginDir in pluginDirs)
            {
                try
                {
                    var pluginId = Path.GetFileName(pluginDir);
                    
                    // 跳过已加载的插件
                    if (_plugins.ContainsKey(pluginId))
                    {
                        continue;
                    }
                    
                    await LoadPluginAsync(pluginDir);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"扫描插件目录失败: {pluginDir}", ex);
                }
            }
        }
        
        /// <summary>
        /// 加载旧版插件（向后兼容）
        /// </summary>
        private async Task LoadLegacyPluginsAsync()
        {
            try
            {
                // 使用旧版PluginManager加载旧版插件
                var legacyManager = new PluginManager(_logger);
                legacyManager.LoadPlugins();
                
                var legacyPlugins = legacyManager.GetAllPlugins();
                
                foreach (var legacyPlugin in legacyPlugins)
                {
                    try
                    {
                        var adapter = new LegacyPluginAdapter(legacyPlugin);
                        var pluginId = adapter.Metadata.Id;
                        
                        // 跳过已加载的插件
                        if (_plugins.ContainsKey(pluginId))
                        {
                            continue;
                        }
                        
                        // 创建插件上下文
                        var context = new PluginContext(
                            _services, _logger, _configuration,
                            pluginId, _pluginsDirectory, _hostVersion);
                        
                        // 初始化适配器
                        await adapter.InitializeAsync(context);
                        
                        // 订阅状态变化事件
                        adapter.StatusChanged += OnPluginStatusChanged;
                        
                        // 注册插件
                        _plugins[pluginId] = adapter;
                        _pluginContexts[pluginId] = context;
                        
                        _logger.LogInfo($"成功加载旧版插件: {adapter.Metadata.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"加载旧版插件失败: {legacyPlugin.Name}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("加载旧版插件失败", ex);
            }
        }
        
        /// <summary>
        /// 处理插件状态变化事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void OnPluginStatusChanged(object? sender, PluginStatusChangedEventArgs e)
        {
            _logger.LogInfo($"插件状态变化: {e.PluginId} {e.OldStatus} -> {e.NewStatus}");
            PluginStatusChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // 停止所有插件
                StopAllPluginsAsync().Wait(TimeSpan.FromSeconds(30));
                
                // 卸载所有插件
                var pluginIds = _plugins.Keys.ToArray();
                foreach (var pluginId in pluginIds)
                {
                    try
                    {
                        UnloadPluginAsync(pluginId).Wait(TimeSpan.FromSeconds(10));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"释放插件失败: {pluginId}", ex);
                    }
                }
                
                _pluginLoader?.Dispose();
                _disposed = true;
            }
        }
    }
}
