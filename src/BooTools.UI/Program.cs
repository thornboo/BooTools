using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BooTools.Core;
using BooTools.Core.Interfaces;
using BooTools.Core.Management;
using BooTools.Core.Repository.Management;
using BooTools.Core.Download;
using BooTools.Core.Package;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

namespace BooTools.UI
{
    static class Program
    {
        // Import the AllocConsole function to show a console window in a WinForms app
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        private static IServiceProvider? _serviceProvider;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            // High DPI and visual styles setup
            if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Application.SetHighDpiMode(HighDpiMode.SystemAware); }
            catch { /* Ignore if not supported */ }

            IPluginManager? pluginManager = null;
            MsLogger? logger = null;

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
                logger = loggerFactory.CreateLogger("BooTools.UI");
                pluginManager = _serviceProvider.GetRequiredService<IPluginManager>();

                logger.LogInformation("BooTools starting up...");
                logger.LogInformation("Plugin manager resolved. Initializing plugins...");

                var initTask = pluginManager.InitializeAsync();
                initTask.Wait();

                if (!initTask.Result.IsSuccess)
                {
                    throw new Exception($"Plugin manager failed to initialize: {initTask.Result.ErrorMessage}");
                }

                logger.LogInformation("Plugins loaded. Starting main form.");
                
                // Resolve MainForm from the service provider to get all dependencies injected
                var mainForm = _serviceProvider.GetRequiredService<MainForm>();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Application startup failed.");
                MessageBox.Show($"A critical error occurred: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                logger?.LogInformation("Application shutting down. Disposing resources...");
                if (pluginManager is IDisposable disposableManager)
                {
                    disposableManager.Dispose();
                }
                
                if (_serviceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
                logger?.LogInformation("Shutdown complete.");
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Application.StartupPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddLogging(configure =>
            {
                configure.ClearProviders(); 
                configure.AddConfiguration(configuration.GetSection("Logging"));
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Debug); 
                
                // Add our custom in-memory logger provider
                configure.AddProvider(new InMemoryLoggerProvider());
            });

            services.AddHttpClient();

            // Register the adapter for any legacy component that needs the old ILogger
            services.AddSingleton<BooTools.Core.ILogger>(provider => {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                // Create a logger with a generic category for the adapter
                return new LoggerAdapter(loggerFactory.CreateLogger("Adapter"));
            });

            services.AddSingleton<IPluginManager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ModernPluginManager>>();
                var config = provider.GetRequiredService<IConfiguration>();
                var hostVersion = new Version(1, 0, 0);
                
                return new ModernPluginManager(provider, logger, config, Application.StartupPath, hostVersion);
            });
            
            services.AddSingleton<PluginRepositoryManager>(provider =>
            {
                var logger = provider.GetRequiredService<BooTools.Core.ILogger>();
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var configDirectory = Path.Combine(Application.StartupPath, "Config");
                return new PluginRepositoryManager(logger, httpClientFactory.CreateClient(), configDirectory);
            });
            
            services.AddSingleton<PluginDownloadManager>(provider =>
            {
                var logger = provider.GetRequiredService<BooTools.Core.ILogger>();
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var downloadDirectory = Path.Combine(Application.StartupPath, "Downloads");
                return new PluginDownloadManager(logger, httpClientFactory.CreateClient(), downloadDirectory);
            });
            
            services.AddSingleton<PluginPackageManager>(provider =>
            {
                var logger = provider.GetRequiredService<BooTools.Core.ILogger>();
                return new PluginPackageManager(logger);
            });

            // Register the MainForm for dependency injection
            services.AddTransient<MainForm>();
        }
    }
}
