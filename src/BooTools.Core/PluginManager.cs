using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BooTools.Core
{
    public class PluginManager
    {
        private readonly List<IBooPlugin> _plugins = new List<IBooPlugin>();
        private readonly ILogger _logger;

        public PluginManager(ILogger? logger = null)
        {
            _logger = logger ?? new FileLogger();
        }

        public void LoadPlugins()
        {
            try
            {
                _logger.LogInfo("开始加载插件...");
                
                var pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                if (!Directory.Exists(pluginsDirectory))
                {
                    _logger.LogWarning($"插件目录不存在: {pluginsDirectory}");
                    return;
                }

                var pluginFiles = Directory.GetFiles(pluginsDirectory, "*.dll");
                _logger.LogInfo($"找到 {pluginFiles.Length} 个插件文件");

                foreach (var pluginFile in pluginFiles)
                {
                    try
                    {
                        LoadPluginFromFile(pluginFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"加载插件文件失败: {pluginFile}", ex);
                    }
                }

                _logger.LogInfo($"插件加载完成，共加载 {_plugins.Count} 个插件");
            }
            catch (Exception ex)
            {
                _logger.LogError("插件管理器初始化失败", ex);
                throw;
            }
        }

        private void LoadPluginFromFile(string pluginPath)
        {
            _logger.LogDebug($"正在加载插件: {pluginPath}");
            
            var assembly = Assembly.LoadFrom(pluginPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IBooPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = (IBooPlugin?)Activator.CreateInstance(pluginType);
                    if (plugin != null)
                    {
                        _plugins.Add(plugin);
                        _logger.LogInfo($"成功加载插件: {plugin.Name} v{plugin.Version}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"创建插件实例失败: {pluginType.Name}", ex);
                }
            }
        }

        public IEnumerable<IBooPlugin> GetAllPlugins()
        {
            return _plugins.AsReadOnly();
        }

        public void StartAllEnabledPlugins()
        {
            _logger.LogInfo("开始启动所有启用的插件...");
            
            foreach (var plugin in _plugins.Where(p => p.IsEnabled))
            {
                try
                {
                    plugin.Start();
                    _logger.LogInfo($"插件 {plugin.Name} 启动成功");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"插件 {plugin.Name} 启动失败", ex);
                }
            }
        }

        public void StopAllPlugins()
        {
            _logger.LogInfo("开始停止所有插件...");
            
            foreach (var plugin in _plugins)
            {
                try
                {
                    plugin.Stop();
                    _logger.LogInfo($"插件 {plugin.Name} 停止成功");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"插件 {plugin.Name} 停止失败", ex);
                }
            }
        }

        public ILogger GetLogger()
        {
            return _logger;
        }

        public bool StartPlugin(string pluginName)
        {
            var plugin = _plugins.FirstOrDefault(p => p.Name == pluginName);
            if (plugin != null)
            {
                try
                {
                    plugin.Start();
                    _logger.LogInfo($"插件 {plugin.Name} 启动成功");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"插件 {plugin.Name} 启动失败", ex);
                    return false;
                }
            }
            return false;
        }

        public bool StopPlugin(string pluginName)
        {
            var plugin = _plugins.FirstOrDefault(p => p.Name == pluginName);
            if (plugin != null)
            {
                try
                {
                    plugin.Stop();
                    _logger.LogInfo($"插件 {plugin.Name} 停止成功");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"插件 {plugin.Name} 停止失败", ex);
                    return false;
                }
            }
            return false;
        }
    }
} 