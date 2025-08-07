using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BooTools.Core.Interfaces;

namespace BooTools.Core.Implementations
{
    /// <summary>
    /// 插件上下文实现
    /// </summary>
    public class PluginContext : IPluginContext
    {
        private readonly IServiceProvider _services;
        private readonly string _pluginId;
        private readonly string _baseDirectory;
        
        /// <summary>
        /// 日志服务
        /// </summary>
        public BooTools.Core.ILogger Logger { get; }
        
        /// <summary>
        /// 配置服务
        /// </summary>
        public IConfiguration Configuration { get; }
        
        /// <summary>
        /// 服务提供者
        /// </summary>
        public IServiceProvider Services => _services;
        
        /// <summary>
        /// 插件安装目录
        /// </summary>
        public string PluginDirectory { get; }
        
        /// <summary>
        /// 插件数据目录
        /// </summary>
        public string DataDirectory { get; }
        
        /// <summary>
        /// 插件配置目录
        /// </summary>
        public string ConfigDirectory { get; }
        
        /// <summary>
        /// 插件临时目录
        /// </summary>
        public string TempDirectory { get; }
        
        /// <summary>
        /// 主机版本
        /// </summary>
        public Version HostVersion { get; }
        
        /// <summary>
        /// 初始化插件上下文
        /// </summary>
        /// <param name="services">服务提供者</param>
        /// <param name="logger">日志服务</param>
        /// <param name="configuration">配置服务</param>
        /// <param name="pluginId">插件ID</param>
        /// <param name="baseDirectory">基础目录</param>
        /// <param name="hostVersion">主机版本</param>
        public PluginContext(
            IServiceProvider services,
            BooTools.Core.ILogger logger,
            IConfiguration configuration,
            string pluginId,
            string baseDirectory,
            Version hostVersion)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _pluginId = pluginId ?? throw new ArgumentNullException(nameof(pluginId));
            _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            HostVersion = hostVersion ?? throw new ArgumentNullException(nameof(hostVersion));
            
            // 设置插件相关目录
            PluginDirectory = Path.Combine(_baseDirectory, "Plugins", _pluginId);
            DataDirectory = Path.Combine(_baseDirectory, "Data", "Plugins", _pluginId);
            ConfigDirectory = Path.Combine(_baseDirectory, "Config", "Plugins", _pluginId);
            TempDirectory = Path.Combine(Path.GetTempPath(), "BooTools", "Plugins", _pluginId);
            
            // 确保目录存在
            EnsureDirectoriesExist();
        }
        
        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public T? GetService<T>() where T : class
        {
            return _services.GetService<T>();
        }
        
        /// <summary>
        /// 获取必需的服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public T GetRequiredService<T>() where T : class
        {
            return _services.GetRequiredService<T>();
        }
        
        /// <summary>
        /// 创建作用域
        /// </summary>
        /// <returns>服务作用域</returns>
        public IServiceScope CreateScope()
        {
            return _services.CreateScope();
        }
        
        /// <summary>
        /// 确保目录存在
        /// </summary>
        private void EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(PluginDirectory);
                Directory.CreateDirectory(DataDirectory);
                Directory.CreateDirectory(ConfigDirectory);
                Directory.CreateDirectory(TempDirectory);
            }
            catch (Exception ex)
            {
                Logger.LogError($"创建插件目录失败: {ex.Message}", ex);
            }
        }
    }
}
