using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BooTools.Core.Interfaces
{
    /// <summary>
    /// 插件上下文接口，为插件提供主机服务
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// 日志服务
        /// </summary>
        BooTools.Core.ILogger Logger { get; }
        
        /// <summary>
        /// 配置服务
        /// </summary>
        IConfiguration Configuration { get; }
        
        /// <summary>
        /// 服务提供者（依赖注入容器）
        /// </summary>
        IServiceProvider Services { get; }
        
        /// <summary>
        /// 插件安装目录
        /// </summary>
        string PluginDirectory { get; }
        
        /// <summary>
        /// 插件数据目录
        /// </summary>
        string DataDirectory { get; }
        
        /// <summary>
        /// 插件配置目录
        /// </summary>
        string ConfigDirectory { get; }
        
        /// <summary>
        /// 插件临时目录
        /// </summary>
        string TempDirectory { get; }
        
        /// <summary>
        /// 主机版本
        /// </summary>
        Version HostVersion { get; }
        
        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        T? GetService<T>() where T : class;
        
        /// <summary>
        /// 获取必需的服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        T GetRequiredService<T>() where T : class;
        
        /// <summary>
        /// 创建作用域
        /// </summary>
        /// <returns>服务作用域</returns>
        IServiceScope CreateScope();
    }
}
