using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BooTools.Core.Interfaces;
using BooTools.Core.Models;

namespace BooTools.Core.Management
{
    /// <summary>
    /// 插件管理器接口
    /// </summary>
    public interface IPluginManager : IDisposable
    {
        /// <summary>
        /// 插件状态变化事件
        /// </summary>
        event EventHandler<PluginStatusChangedEventArgs> PluginStatusChanged;
        
        /// <summary>
        /// 初始化插件管理器
        /// </summary>
        /// <returns>初始化结果</returns>
        Task<PluginResult> InitializeAsync();
        
        /// <summary>
        /// 获取所有已安装的插件
        /// </summary>
        /// <returns>插件列表</returns>
        Task<IEnumerable<IPlugin>> GetInstalledPluginsAsync();
        
        /// <summary>
        /// 根据ID获取插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>插件实例</returns>
        Task<IPlugin?> GetPluginAsync(string pluginId);
        
        /// <summary>
        /// 加载插件
        /// </summary>
        /// <param name="pluginPath">插件路径</param>
        /// <param name="entryAssemblyName">入口程序集名称</param>
        /// <returns>加载结果</returns>
        Task<PluginResult<IPlugin>> LoadPluginAsync(string pluginPath, string? entryAssemblyName = null);
        
        /// <summary>
        /// 卸载插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>卸载结果</returns>
        Task<PluginResult> UnloadPluginAsync(string pluginId);
        
        /// <summary>
        /// 启动插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>启动结果</returns>
        Task<PluginResult> StartPluginAsync(string pluginId);
        
        /// <summary>
        /// 停止插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>停止结果</returns>
        Task<PluginResult> StopPluginAsync(string pluginId);
        
        /// <summary>
        /// 启动所有启用的插件
        /// </summary>
        /// <returns>启动结果</returns>
        Task<PluginResult> StartAllEnabledPluginsAsync();
        
        /// <summary>
        /// 停止所有插件
        /// </summary>
        /// <returns>停止结果</returns>
        Task<PluginResult> StopAllPluginsAsync();
        
        /// <summary>
        /// 刷新插件列表
        /// </summary>
        /// <returns>刷新结果</returns>
        Task<PluginResult> RefreshPluginsAsync();
        
        /// <summary>
        /// 设置插件启用状态
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="enabled">是否启用</param>
        /// <returns>设置结果</returns>
        Task<PluginResult> SetPluginEnabledAsync(string pluginId, bool enabled);
        
        /// <summary>
        /// 获取插件统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        Task<PluginStatistics> GetStatisticsAsync();
    }
    
    /// <summary>
    /// 插件统计信息
    /// </summary>
    public class PluginStatistics
    {
        /// <summary>
        /// 总插件数量
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// 运行中的插件数量
        /// </summary>
        public int RunningCount { get; set; }
        
        /// <summary>
        /// 启用的插件数量
        /// </summary>
        public int EnabledCount { get; set; }
        
        /// <summary>
        /// 错误状态的插件数量
        /// </summary>
        public int ErrorCount { get; set; }
        
        /// <summary>
        /// 旧版插件数量
        /// </summary>
        public int LegacyCount { get; set; }
        
        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
