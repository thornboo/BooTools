using System;
using System.Threading.Tasks;
using BooTools.Core.Interfaces;
using BooTools.Core.Models;

namespace BooTools.Core.Compatibility
{
    /// <summary>
    /// 旧版插件适配器，用于兼容IBooPlugin接口
    /// </summary>
    public class LegacyPluginAdapter : IPlugin
    {
        private readonly IBooPlugin _legacyPlugin;
        private PluginStatus _status = PluginStatus.Installed;
        private IPluginContext? _context;
        
        /// <summary>
        /// 插件元数据
        /// </summary>
        public PluginMetadata Metadata { get; }
        
        /// <summary>
        /// 插件状态
        /// </summary>
        public PluginStatus Status => _status;
        
        /// <summary>
        /// 插件状态变化事件
        /// </summary>
        public event EventHandler<PluginStatusChangedEventArgs>? StatusChanged;
        
        /// <summary>
        /// 初始化旧版插件适配器
        /// </summary>
        /// <param name="legacyPlugin">旧版插件实例</param>
        public LegacyPluginAdapter(IBooPlugin legacyPlugin)
        {
            _legacyPlugin = legacyPlugin ?? throw new ArgumentNullException(nameof(legacyPlugin));
            
            // 将旧版插件信息转换为新版元数据
            Metadata = new PluginMetadata
            {
                Id = GeneratePluginId(_legacyPlugin.Name),
                Name = _legacyPlugin.Name,
                Description = _legacyPlugin.Description,
                Version = Version.TryParse(_legacyPlugin.Version, out var version) ? version : new Version(1, 0, 0),
                Author = _legacyPlugin.Author,
                IconPath = _legacyPlugin.IconPath,
                Category = "Legacy",
                Tags = { "legacy", "compatibility" }
            };
            
            // 订阅旧版插件状态变化事件
            _legacyPlugin.StatusChanged += OnLegacyPluginStatusChanged;
        }
        
        /// <summary>
        /// 初始化插件
        /// </summary>
        /// <param name="context">插件上下文</param>
        /// <returns>初始化结果</returns>
        public async Task<PluginResult> InitializeAsync(IPluginContext context)
        {
            try
            {
                _context = context;
                SetStatus(PluginStatus.Initializing);
                
                await Task.Run(() => _legacyPlugin.Initialize());
                
                SetStatus(PluginStatus.Initialized);
                return PluginResult.Success("旧版插件初始化成功");
            }
            catch (Exception ex)
            {
                SetStatus(PluginStatus.Error);
                return PluginResult.Failure($"旧版插件初始化失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 启动插件
        /// </summary>
        /// <returns>启动结果</returns>
        public async Task<PluginResult> StartAsync()
        {
            try
            {
                SetStatus(PluginStatus.Starting);
                
                await Task.Run(() => _legacyPlugin.Start());
                
                SetStatus(PluginStatus.Running);
                return PluginResult.Success("旧版插件启动成功");
            }
            catch (Exception ex)
            {
                SetStatus(PluginStatus.Error);
                return PluginResult.Failure($"旧版插件启动失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 停止插件
        /// </summary>
        /// <returns>停止结果</returns>
        public async Task<PluginResult> StopAsync()
        {
            try
            {
                SetStatus(PluginStatus.Stopping);
                
                await Task.Run(() => _legacyPlugin.Stop());
                
                SetStatus(PluginStatus.Stopped);
                return PluginResult.Success("旧版插件停止成功");
            }
            catch (Exception ex)
            {
                SetStatus(PluginStatus.Error);
                return PluginResult.Failure($"旧版插件停止失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 卸载插件
        /// </summary>
        /// <returns>卸载结果</returns>
        public async Task<PluginResult> UnloadAsync()
        {
            try
            {
                SetStatus(PluginStatus.Unloading);
                
                // 旧版插件没有Unload方法，先停止然后清理事件
                if (_status == PluginStatus.Running)
                {
                    await StopAsync();
                }
                
                _legacyPlugin.StatusChanged -= OnLegacyPluginStatusChanged;
                
                SetStatus(PluginStatus.Unloaded);
                return PluginResult.Success("旧版插件卸载成功");
            }
            catch (Exception ex)
            {
                SetStatus(PluginStatus.Error);
                return PluginResult.Failure($"旧版插件卸载失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 显示插件设置界面
        /// </summary>
        public void ShowSettings()
        {
            try
            {
                _legacyPlugin.ShowSettings();
            }
            catch (Exception ex)
            {
                _context?.Logger.LogError($"显示旧版插件设置失败: {Metadata.Name}", ex);
            }
        }
        
        /// <summary>
        /// 获取插件配置模式
        /// </summary>
        /// <returns>配置模式</returns>
        public PluginConfigurationMode GetConfigurationMode()
        {
            // 旧版插件默认使用高级配置模式
            return PluginConfigurationMode.Advanced;
        }
        
        /// <summary>
        /// 验证插件依赖
        /// </summary>
        /// <param name="context">插件上下文</param>
        /// <returns>验证结果</returns>
        public async Task<PluginResult> ValidateDependenciesAsync(IPluginContext context)
        {
            // 旧版插件没有依赖声明，默认通过验证
            await Task.CompletedTask;
            return PluginResult.Success("旧版插件依赖验证通过");
        }
        
        /// <summary>
        /// 设置插件状态
        /// </summary>
        /// <param name="newStatus">新状态</param>
        private void SetStatus(PluginStatus newStatus)
        {
            var oldStatus = _status;
            _status = newStatus;
            
            StatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(
                Metadata.Id, oldStatus, newStatus, "状态由适配器管理"));
        }
        
        /// <summary>
        /// 处理旧版插件状态变化
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="isEnabled">是否启用</param>
        private void OnLegacyPluginStatusChanged(object? sender, bool isEnabled)
        {
            // 将旧版插件的布尔状态转换为新版状态枚举
            var newStatus = isEnabled ? PluginStatus.Running : PluginStatus.Stopped;
            if (newStatus != _status)
            {
                SetStatus(newStatus);
            }
        }
        
        /// <summary>
        /// 生成插件ID
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        /// <returns>插件ID</returns>
        private static string GeneratePluginId(string pluginName)
        {
            // 将插件名称转换为ID格式
            return pluginName.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                .Replace(".", "-");
        }
    }
}
