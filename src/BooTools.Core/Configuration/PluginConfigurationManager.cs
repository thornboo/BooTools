using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BooTools.Core.Configuration
{
    /// <summary>
    /// 插件配置管理器
    /// </summary>
    public class PluginConfigurationManager
    {
        private readonly BooTools.Core.ILogger _logger;
        private readonly string _configDirectory;
        private readonly ConcurrentDictionary<string, PluginConfiguration> _configurations;
        private readonly JsonSerializerOptions _jsonOptions;
        
        /// <summary>
        /// 初始化插件配置管理器
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="configDirectory">配置目录</param>
        public PluginConfigurationManager(BooTools.Core.ILogger logger, string configDirectory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configDirectory = configDirectory ?? throw new ArgumentNullException(nameof(configDirectory));
            _configurations = new ConcurrentDictionary<string, PluginConfiguration>();
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            
            // 确保配置目录存在
            Directory.CreateDirectory(_configDirectory);
        }
        
        /// <summary>
        /// 获取插件配置
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>插件配置</returns>
        public async Task<PluginConfiguration> GetConfigurationAsync(string pluginId)
        {
            if (_configurations.TryGetValue(pluginId, out var cachedConfig))
            {
                return cachedConfig;
            }
            
            var config = await LoadConfigurationAsync(pluginId);
            _configurations[pluginId] = config;
            return config;
        }
        
        /// <summary>
        /// 保存插件配置
        /// </summary>
        /// <param name="configuration">插件配置</param>
        /// <returns>保存结果</returns>
        public async Task<bool> SaveConfigurationAsync(PluginConfiguration configuration)
        {
            try
            {
                configuration.LastUpdated = DateTime.Now;
                _configurations[configuration.PluginId] = configuration;
                
                var configPath = GetConfigurationPath(configuration.PluginId);
                var json = JsonSerializer.Serialize(configuration, _jsonOptions);
                await File.WriteAllTextAsync(configPath, json);
                
                _logger.LogInfo($"保存插件配置成功: {configuration.PluginId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"保存插件配置失败: {configuration.PluginId}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 删除插件配置
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>删除结果</returns>
        public async Task<bool> DeleteConfigurationAsync(string pluginId)
        {
            try
            {
                _configurations.TryRemove(pluginId, out _);
                
                var configPath = GetConfigurationPath(pluginId);
                if (File.Exists(configPath))
                {
                    await Task.Run(() => File.Delete(configPath));
                }
                
                _logger.LogInfo($"删除插件配置成功: {pluginId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"删除插件配置失败: {pluginId}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 获取所有插件配置
        /// </summary>
        /// <returns>插件配置列表</returns>
        public async Task<IEnumerable<PluginConfiguration>> GetAllConfigurationsAsync()
        {
            var configFiles = Directory.GetFiles(_configDirectory, "*.json");
            var configurations = new List<PluginConfiguration>();
            
            foreach (var configFile in configFiles)
            {
                try
                {
                    var pluginId = Path.GetFileNameWithoutExtension(configFile);
                    var config = await GetConfigurationAsync(pluginId);
                    configurations.Add(config);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"加载插件配置失败: {configFile}", ex);
                }
            }
            
            return configurations;
        }
        
        /// <summary>
        /// 获取启用的插件配置
        /// </summary>
        /// <returns>启用的插件配置列表</returns>
        public async Task<IEnumerable<PluginConfiguration>> GetEnabledConfigurationsAsync()
        {
            var allConfigs = await GetAllConfigurationsAsync();
            return allConfigs.Where(c => c.IsEnabled);
        }
        
        /// <summary>
        /// 设置插件启用状态
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="enabled">是否启用</param>
        /// <returns>设置结果</returns>
        public async Task<bool> SetPluginEnabledAsync(string pluginId, bool enabled)
        {
            try
            {
                var config = await GetConfigurationAsync(pluginId);
                config.IsEnabled = enabled;
                return await SaveConfigurationAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError($"设置插件启用状态失败: {pluginId}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 设置插件自动启动状态
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="autoStart">是否自动启动</param>
        /// <returns>设置结果</returns>
        public async Task<bool> SetPluginAutoStartAsync(string pluginId, bool autoStart)
        {
            try
            {
                var config = await GetConfigurationAsync(pluginId);
                config.AutoStart = autoStart;
                return await SaveConfigurationAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError($"设置插件自动启动状态失败: {pluginId}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 备份插件配置
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="backupPath">备份路径</param>
        /// <returns>备份结果</returns>
        public async Task<bool> BackupConfigurationAsync(string pluginId, string backupPath)
        {
            try
            {
                var configPath = GetConfigurationPath(pluginId);
                if (File.Exists(configPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                    await Task.Run(() => File.Copy(configPath, backupPath, true));
                    _logger.LogInfo($"备份插件配置成功: {pluginId} -> {backupPath}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"备份插件配置失败: {pluginId}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 恢复插件配置
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="backupPath">备份路径</param>
        /// <returns>恢复结果</returns>
        public async Task<bool> RestoreConfigurationAsync(string pluginId, string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    var configPath = GetConfigurationPath(pluginId);
                    await Task.Run(() => File.Copy(backupPath, configPath, true));
                    
                    // 重新加载配置到缓存
                    _configurations.TryRemove(pluginId, out _);
                    await GetConfigurationAsync(pluginId);
                    
                    _logger.LogInfo($"恢复插件配置成功: {pluginId} <- {backupPath}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"恢复插件配置失败: {pluginId}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 加载插件配置
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>插件配置</returns>
        private async Task<PluginConfiguration> LoadConfigurationAsync(string pluginId)
        {
            var configPath = GetConfigurationPath(pluginId);
            
            if (!File.Exists(configPath))
            {
                // 创建默认配置
                var defaultConfig = new PluginConfiguration
                {
                    PluginId = pluginId,
                    IsEnabled = true,
                    AutoStart = true
                };
                
                await SaveConfigurationAsync(defaultConfig);
                return defaultConfig;
            }
            
            try
            {
                var json = await File.ReadAllTextAsync(configPath);
                var config = JsonSerializer.Deserialize<PluginConfiguration>(json, _jsonOptions);
                
                if (config == null)
                {
                    throw new InvalidOperationException("反序列化配置失败");
                }
                
                // 确保插件ID正确
                config.PluginId = pluginId;
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError($"加载插件配置失败: {pluginId}", ex);
                
                // 返回默认配置
                return new PluginConfiguration
                {
                    PluginId = pluginId,
                    IsEnabled = true,
                    AutoStart = true
                };
            }
        }
        
        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>配置文件路径</returns>
        private string GetConfigurationPath(string pluginId)
        {
            return Path.Combine(_configDirectory, $"{pluginId}.json");
        }
    }
}
