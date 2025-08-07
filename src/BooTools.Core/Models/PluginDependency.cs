using System;

namespace BooTools.Core.Models
{
    /// <summary>
    /// 插件依赖信息
    /// </summary>
    public class PluginDependency
    {
        /// <summary>
        /// 依赖ID（与Name相同，为了兼容性）
        /// </summary>
        public string Id => Name;
        
        /// <summary>
        /// 依赖名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 依赖版本
        /// </summary>
        public Version Version { get; set; } = new Version(1, 0, 0);
        
        /// <summary>
        /// 最低版本要求
        /// </summary>
        public Version? MinVersion { get; set; }
        
        /// <summary>
        /// 最高版本支持
        /// </summary>
        public Version? MaxVersion { get; set; }
        
        /// <summary>
        /// 是否为可选依赖
        /// </summary>
        public bool IsOptional { get; set; } = false;
        
        /// <summary>
        /// 依赖类型
        /// </summary>
        public DependencyType Type { get; set; } = DependencyType.Plugin;
    }
    
    /// <summary>
    /// 依赖类型枚举
    /// </summary>
    public enum DependencyType
    {
        /// <summary>
        /// 插件依赖
        /// </summary>
        Plugin,
        
        /// <summary>
        /// 程序集依赖
        /// </summary>
        Assembly,
        
        /// <summary>
        /// NuGet包依赖
        /// </summary>
        NuGetPackage,
        
        /// <summary>
        /// 系统依赖
        /// </summary>
        System
    }
}
