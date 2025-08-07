using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BooTools.Core.Interfaces;
using BooTools.Core.Models;

namespace BooTools.Core.PluginLoading
{
    /// <summary>
    /// 插件加载器
    /// </summary>
    public class PluginLoader : IDisposable
    {
        private readonly BooTools.Core.ILogger _logger;
        private readonly Dictionary<string, PluginLoadContext> _loadContexts;
        private bool _disposed = false;
        
        /// <summary>
        /// 初始化插件加载器
        /// </summary>
        /// <param name="logger">日志服务</param>
        public PluginLoader(BooTools.Core.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loadContexts = new Dictionary<string, PluginLoadContext>();
        }
        
        /// <summary>
        /// 加载插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="pluginPath">插件路径</param>
        /// <param name="entryAssemblyName">入口程序集名称</param>
        /// <returns>插件实例</returns>
        public Task<PluginResult<IPlugin>> LoadPluginAsync(string pluginId, string pluginPath, string entryAssemblyName)
        {
            try
            {
                _logger.LogInfo($"开始加载插件: {pluginId}, 路径: {pluginPath}");
                
                // 检查插件路径
                if (!Directory.Exists(pluginPath))
                {
                    return Task.FromResult(PluginResult<IPlugin>.Failure($"插件路径不存在: {pluginPath}"));
                }
                
                // 查找入口程序集
                string entryAssemblyPath = Path.Combine(pluginPath, entryAssemblyName);
                if (!File.Exists(entryAssemblyPath))
                {
                    // 尝试查找第一个匹配的DLL文件
                    var dllFiles = Directory.GetFiles(pluginPath, "*.dll")
                        .Where(f => !Path.GetFileName(f).StartsWith("BooTools.Core"))
                        .ToArray();
                    
                    if (dllFiles.Length == 0)
                    {
                        return Task.FromResult(PluginResult<IPlugin>.Failure($"在插件目录中未找到程序集文件: {pluginPath}"));
                    }
                    
                    entryAssemblyPath = dllFiles[0];
                    _logger.LogInfo($"自动选择入口程序集: {Path.GetFileName(entryAssemblyPath)}");
                }
                
                // 创建插件加载上下文
                var loadContext = new PluginLoadContext(pluginId, pluginPath);
                _loadContexts[pluginId] = loadContext;
                
                // 加载插件程序集
                Assembly pluginAssembly = loadContext.LoadPluginAssembly(entryAssemblyPath);
                _logger.LogInfo($"成功加载插件程序集: {pluginAssembly.FullName}");
                
                // 查找插件类型
                var pluginTypes = FindPluginTypes(pluginAssembly);
                if (pluginTypes.Count == 0)
                {
                    return Task.FromResult(PluginResult<IPlugin>.Failure($"在程序集中未找到实现IPlugin接口的类型: {pluginAssembly.FullName}"));
                }
                
                if (pluginTypes.Count > 1)
                {
                    _logger.LogWarning($"在程序集中找到多个插件类型，使用第一个: {pluginTypes[0].FullName}");
                }
                
                // 创建插件实例
                var pluginType = pluginTypes[0];
                var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                
                if (plugin == null)
                {
                    return Task.FromResult(PluginResult<IPlugin>.Failure($"无法创建插件实例: {pluginType.FullName}"));
                }
                
                _logger.LogInfo($"成功加载插件: {plugin.Metadata.Name} v{plugin.Metadata.Version}");
                return Task.FromResult(PluginResult<IPlugin>.Success(plugin, "插件加载成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"加载插件失败: {pluginId}", ex);
                return Task.FromResult(PluginResult<IPlugin>.Failure($"加载插件失败: {ex.Message}", ex));
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
                if (!_loadContexts.TryGetValue(pluginId, out var loadContext))
                {
                    return PluginResult.Success($"插件 {pluginId} 未加载或已卸载");
                }
                
                _logger.LogInfo($"开始卸载插件: {pluginId}");
                
                // 卸载加载上下文
                loadContext.Unload();
                _loadContexts.Remove(pluginId);
                
                // 等待垃圾回收
                await Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                });
                
                _logger.LogInfo($"成功卸载插件: {pluginId}");
                return PluginResult.Success("插件卸载成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"卸载插件失败: {pluginId}", ex);
                return PluginResult.Failure($"卸载插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 查找插件类型
        /// </summary>
        /// <param name="assembly">程序集</param>
        /// <returns>插件类型列表</returns>
        private List<Type> FindPluginTypes(Assembly assembly)
        {
            var pluginTypes = new List<Type>();
            
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (typeof(IPlugin).IsAssignableFrom(type) && 
                        !type.IsInterface && 
                        !type.IsAbstract &&
                        type.GetConstructor(Type.EmptyTypes) != null)
                    {
                        pluginTypes.Add(type);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning($"加载程序集类型时出现部分错误: {assembly.FullName}");
                
                // 尝试加载成功的类型
                foreach (var type in ex.Types)
                {
                    if (type != null && 
                        typeof(IPlugin).IsAssignableFrom(type) && 
                        !type.IsInterface && 
                        !type.IsAbstract)
                    {
                        pluginTypes.Add(type);
                    }
                }
            }
            
            return pluginTypes;
        }
        
        /// <summary>
        /// 获取已加载的插件上下文
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>插件加载上下文</returns>
        public PluginLoadContext? GetLoadContext(string pluginId)
        {
            return _loadContexts.TryGetValue(pluginId, out var context) ? context : null;
        }
        
        /// <summary>
        /// 获取所有已加载的插件ID
        /// </summary>
        /// <returns>插件ID列表</returns>
        public IEnumerable<string> GetLoadedPluginIds()
        {
            return _loadContexts.Keys.ToArray();
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var context in _loadContexts.Values)
                {
                    try
                    {
                        context.Unload();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"卸载插件上下文时发生错误: {context.PluginId}", ex);
                    }
                }
                
                _loadContexts.Clear();
                _disposed = true;
            }
        }
    }
}
