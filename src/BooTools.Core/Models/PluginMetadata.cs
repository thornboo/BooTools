using System;
using System.Collections.Generic;

namespace BooTools.Core.Models
{
    /// <summary>
    /// 插件元数据信息
    /// </summary>
    public class PluginMetadata
    {
        /// <summary>
        /// 插件唯一标识符
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// 插件显示名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 插件描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 插件版本
        /// </summary>
        public Version Version { get; set; } = new Version(1, 0, 0);
        
        /// <summary>
        /// 插件作者
        /// </summary>
        public string Author { get; set; } = string.Empty;
        
        /// <summary>
        /// 插件图标路径
        /// </summary>
        public string IconPath { get; set; } = string.Empty;
        
        /// <summary>
        /// 插件分类
        /// </summary>
        public string Category { get; set; } = "其他";
        
        /// <summary>
        /// 插件标签
        /// </summary>
        public List<string> Tags { get; set; } = new();
        
        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime PublishTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 插件依赖
        /// </summary>
        public List<PluginDependency> Dependencies { get; set; } = new List<PluginDependency>();
        
        /// <summary>
        /// 所需权限
        /// </summary>
        public List<string> RequiredPermissions { get; set; } = new List<string>();
        
        /// <summary>
        /// 最低主机版本要求
        /// </summary>
        public Version? MinHostVersion { get; set; }
        
        /// <summary>
        /// 最高主机版本支持
        /// </summary>
        public Version? MaxHostVersion { get; set; }
        
        /// <summary>
        /// 下载地址
        /// </summary>
        public string? DownloadUrl { get; set; }
        
        /// <summary>
        /// 文件校验和
        /// </summary>
        public string? Checksum { get; set; }
        
        /// <summary>
        /// 插件大小（字节）
        /// </summary>
        public long Size { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 插件主页URL
        /// </summary>
        public string? HomepageUrl { get; set; }
        
        /// <summary>
        /// 插件源码URL
        /// </summary>
        public string? SourceUrl { get; set; }
        
        /// <summary>
        /// 许可证
        /// </summary>
        public string License { get; set; } = "MIT";
        
        /// <summary>
        /// 最低 .NET 版本要求
        /// </summary>
        public string MinimumDotNetVersion { get; set; } = "8.0";
    }
}
