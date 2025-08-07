using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using BooTools.Core;
using BooTools.Core.Management;
using BooToolsLogger = BooTools.Core.ILogger;

namespace BooTools.UI
{
    static class Program
    {
        private static IPluginManager? _pluginManager;
        private static BooToolsLogger? _logger;
        private static IServiceProvider? _services;
        
        // 导入Windows API以支持DPI感知
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        
        [STAThread]
        static void Main()
        {
            // 启用高DPI支持
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }
            
            // 启用应用程序视觉样式和现代渲染
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // 设置高DPI模式 (.NET Core 3.0+)
            try
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
            }
            catch
            {
                // 如果不支持，则忽略
            }
            
            try
            {
                // 初始化日志系统
                _logger = new ConsoleLogger(true); // 启用控制台输出
                _logger.LogInfo("BooTools 程序启动");
                
                // 初始化依赖注入容器
                var services = new ServiceCollection();
                ConfigureServices(services);
                _services = services.BuildServiceProvider();
                
                // 初始化配置
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Application.StartupPath)
                    .AddJsonFile("appsettings.json", optional: true)
                    .Build();
                
                // 初始化现代插件管理器
                _pluginManager = new ModernPluginManager(
                    _services, _logger, configuration, 
                    Application.StartupPath, new Version(1, 0, 0));
                
                _logger.LogInfo("插件管理器初始化完成");
                
                // 异步初始化插件管理器
                var initTask = _pluginManager.InitializeAsync();
                initTask.Wait();
                
                if (!initTask.Result.IsSuccess)
                {
                    throw new Exception($"插件管理器初始化失败: {initTask.Result.ErrorMessage}");
                }
                
                _logger.LogInfo("插件加载完成，启动主界面");
                
                // 启动主界面
                Application.Run(new MainForm(_pluginManager, _services));
            }
            catch (Exception ex)
            {
                _logger?.LogError("程序启动失败", ex);
                MessageBox.Show($"程序启动失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 清理资源
                _logger?.LogInfo("程序正在退出，清理资源...");
                _pluginManager?.StopAllPluginsAsync().Wait(TimeSpan.FromSeconds(10));
                _logger?.LogInfo("程序退出完成");
                
                // 释放服务容器
                (_services as IDisposable)?.Dispose();
            }
        }
        
        /// <summary>
        /// 配置依赖注入服务
        /// </summary>
        /// <param name="services">服务集合</param>
        private static void ConfigureServices(IServiceCollection services)
        {
            // 注册日志服务
            services.AddSingleton<BooToolsLogger>(provider => _logger ?? new ConsoleLogger());
            
            // 注册配置服务
            services.AddSingleton<IConfiguration>(provider =>
            {
                return new ConfigurationBuilder()
                    .SetBasePath(Application.StartupPath)
                    .AddJsonFile("appsettings.json", optional: true)
                    .Build();
            });
            
            // 注册HTTP客户端
            services.AddHttpClient();
            
            // 注册远程插件管理服务
            services.AddSingleton<BooTools.Core.Repository.Management.PluginRepositoryManager>(provider =>
            {
                var logger = provider.GetRequiredService<BooToolsLogger>();
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                var configDirectory = Path.Combine(Application.StartupPath, "Config");
                return new BooTools.Core.Repository.Management.PluginRepositoryManager(logger, httpClient, configDirectory);
            });
            
            services.AddSingleton<BooTools.Core.Download.PluginDownloadManager>(provider =>
            {
                var logger = provider.GetRequiredService<BooToolsLogger>();
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                var downloadDirectory = Path.Combine(Application.StartupPath, "Downloads");
                return new BooTools.Core.Download.PluginDownloadManager(logger, httpClient, downloadDirectory);
            });
            
            services.AddSingleton<BooTools.Core.Package.PluginPackageManager>(provider =>
            {
                var logger = provider.GetRequiredService<BooToolsLogger>();
                return new BooTools.Core.Package.PluginPackageManager(logger);
            });
        }
    }
} 