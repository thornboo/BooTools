using System;
using System.Threading.Tasks;
using BooTools.Core.Models;

namespace BooTools.Core.Interfaces
{
    /// <summary>
    /// 新版插件接口
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// 插件元数据
        /// </summary>
        PluginMetadata Metadata { get; }
        
        /// <summary>
        /// 插件当前状态
        /// </summary>
        PluginStatus Status { get; }
        
        /// <summary>
        /// 初始化插件
        /// </summary>
        /// <param name="context">插件上下文</param>
        /// <returns>初始化结果</returns>
        Task<PluginResult> InitializeAsync(IPluginContext context);
        
        /// <summary>
        /// 启动插件
        /// </summary>
        /// <returns>启动结果</returns>
        Task<PluginResult> StartAsync();
        
        /// <summary>
        /// 停止插件
        /// </summary>
        /// <returns>停止结果</returns>
        Task<PluginResult> StopAsync();
        
        /// <summary>
        /// 卸载插件
        /// </summary>
        /// <returns>卸载结果</returns>
        Task<PluginResult> UnloadAsync();
        
        /// <summary>
        /// 显示插件设置界面
        /// </summary>
        void ShowSettings();
        
        /// <summary>
        /// 插件状态变化事件
        /// </summary>
        event EventHandler<PluginStatusChangedEventArgs> StatusChanged;
        
        /// <summary>
        /// 获取插件配置模式
        /// </summary>
        /// <returns>配置模式</returns>
        PluginConfigurationMode GetConfigurationMode();
        
        /// <summary>
        /// 验证插件依赖
        /// </summary>
        /// <param name="context">插件上下文</param>
        /// <returns>验证结果</returns>
        Task<PluginResult> ValidateDependenciesAsync(IPluginContext context);
    }
    
    /// <summary>
    /// 插件配置模式
    /// </summary>
    public enum PluginConfigurationMode
    {
        /// <summary>
        /// 无需配置
        /// </summary>
        None,
        
        /// <summary>
        /// 简单配置（键值对）
        /// </summary>
        Simple,
        
        /// <summary>
        /// 高级配置（自定义界面）
        /// </summary>
        Advanced,
        
        /// <summary>
        /// 向导配置
        /// </summary>
        Wizard
    }
}
