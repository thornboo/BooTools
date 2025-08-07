using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace BooTools.Core.PluginLoading
{
    /// <summary>
    /// 插件加载上下文，实现插件隔离
    /// </summary>
    public class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly string _pluginPath;
        
        /// <summary>
        /// 插件ID
        /// </summary>
        public string PluginId { get; }
        
        /// <summary>
        /// 插件路径
        /// </summary>
        public string PluginPath => _pluginPath;
        
        /// <summary>
        /// 初始化插件加载上下文
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="pluginPath">插件路径</param>
        public PluginLoadContext(string pluginId, string pluginPath) : base(pluginId, true)
        {
            PluginId = pluginId ?? throw new ArgumentNullException(nameof(pluginId));
            _pluginPath = pluginPath ?? throw new ArgumentNullException(nameof(pluginPath));
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }
        
        /// <summary>
        /// 加载程序集
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns>程序集</returns>
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // 首先尝试从插件目录解析
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            
            // 如果是系统程序集或共享程序集，使用默认加载上下文
            if (IsSharedAssembly(assemblyName))
            {
                return null; // 返回null让默认上下文处理
            }
            
            return null;
        }
        
        /// <summary>
        /// 加载非托管库
        /// </summary>
        /// <param name="unmanagedDllName">非托管库名称</param>
        /// <returns>库句柄</returns>
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
            
            return IntPtr.Zero;
        }
        
        /// <summary>
        /// 加载插件主程序集
        /// </summary>
        /// <param name="assemblyPath">程序集路径</param>
        /// <returns>程序集</returns>
        public Assembly LoadPluginAssembly(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"插件程序集文件不存在: {assemblyPath}");
            }
            
            return LoadFromAssemblyPath(assemblyPath);
        }
        
        /// <summary>
        /// 判断是否为共享程序集
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns>是否为共享程序集</returns>
        private static bool IsSharedAssembly(AssemblyName assemblyName)
        {
            // 这些程序集应该在默认上下文中共享
            string[] sharedAssemblies = {
                "System",
                "System.Runtime",
                "System.Core",
                "mscorlib",
                "netstandard",
                "Microsoft.Extensions.DependencyInjection",
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Configuration",
                "BooTools.Core" // 核心程序集也要共享
            };
            
            string name = assemblyName.Name ?? string.Empty;
            
            foreach (string sharedAssembly in sharedAssemblies)
            {
                if (name.StartsWith(sharedAssembly, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取插件目录中的所有程序集
        /// </summary>
        /// <returns>程序集路径数组</returns>
        public string[] GetPluginAssemblies()
        {
            if (!Directory.Exists(_pluginPath))
            {
                return Array.Empty<string>();
            }
            
            return Directory.GetFiles(_pluginPath, "*.dll", SearchOption.AllDirectories);
        }
    }
}
