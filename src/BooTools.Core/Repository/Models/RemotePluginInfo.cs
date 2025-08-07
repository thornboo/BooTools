using System;
using System.Collections.Generic;
using BooTools.Core.Models;

namespace BooTools.Core.Repository.Models
{
    /// <summary>
    /// 远程插件信息
    /// </summary>
    public class RemotePluginInfo
    {
        /// <summary>
        /// 插件基本元数据
        /// </summary>
        public PluginMetadata Metadata { get; set; } = new();
        
        /// <summary>
        /// 来源仓库ID
        /// </summary>
        public string RepositoryId { get; set; } = string.Empty;
        
        /// <summary>
        /// 来源仓库名称
        /// </summary>
        public string RepositoryName { get; set; } = string.Empty;
        
        /// <summary>
        /// 下载统计
        /// </summary>
        public DownloadStatistics Downloads { get; set; } = new();
        
        /// <summary>
        /// 评分信息
        /// </summary>
        public RatingInfo Rating { get; set; } = new();
        
        /// <summary>
        /// 版本历史
        /// </summary>
        public List<PluginVersionInfo> Versions { get; set; } = new();
        
        /// <summary>
        /// 屏幕截图
        /// </summary>
        public List<string> Screenshots { get; set; } = new();
        
        /// <summary>
        /// 详细描述（支持Markdown）
        /// </summary>
        public string DetailedDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// 更新日志
        /// </summary>
        public string Changelog { get; set; } = string.Empty;
        
        /// <summary>
        /// 系统要求
        /// </summary>
        public SystemRequirements Requirements { get; set; } = new();
        
        /// <summary>
        /// 发布状态
        /// </summary>
        public ReleaseStatus Status { get; set; } = ReleaseStatus.Stable;
        
        /// <summary>
        /// 是否为付费插件
        /// </summary>
        public bool IsPaid { get; set; } = false;
        
        /// <summary>
        /// 价格信息
        /// </summary>
        public decimal Price { get; set; } = 0;
        
        /// <summary>
        /// 货币单位
        /// </summary>
        public string Currency { get; set; } = "CNY";
        
        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 验证状态
        /// </summary>
        public VerificationStatus Verification { get; set; } = VerificationStatus.Unverified;
    }
    
    /// <summary>
    /// 下载统计信息
    /// </summary>
    public class DownloadStatistics
    {
        /// <summary>
        /// 总下载次数
        /// </summary>
        public long TotalDownloads { get; set; } = 0;
        
        /// <summary>
        /// 本周下载次数
        /// </summary>
        public long WeeklyDownloads { get; set; } = 0;
        
        /// <summary>
        /// 本月下载次数
        /// </summary>
        public long MonthlyDownloads { get; set; } = 0;
        
        /// <summary>
        /// 最后下载时间
        /// </summary>
        public DateTime LastDownloadTime { get; set; } = DateTime.MinValue;
    }
    
    /// <summary>
    /// 评分信息
    /// </summary>
    public class RatingInfo
    {
        /// <summary>
        /// 平均评分（1-5星）
        /// </summary>
        public double AverageRating { get; set; } = 0;
        
        /// <summary>
        /// 评分总数
        /// </summary>
        public int TotalRatings { get; set; } = 0;
        
        /// <summary>
        /// 各星级评分分布
        /// </summary>
        public Dictionary<int, int> RatingDistribution { get; set; } = new()
        {
            { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
        };
    }
    
    /// <summary>
    /// 插件版本信息
    /// </summary>
    public class PluginVersionInfo
    {
        /// <summary>
        /// 版本号
        /// </summary>
        public Version Version { get; set; } = new Version(1, 0, 0);
        
        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime ReleaseDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 版本描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 下载URL
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; } = 0;
        
        /// <summary>
        /// 文件校验和
        /// </summary>
        public string Checksum { get; set; } = string.Empty;
        
        /// <summary>
        /// 校验和类型
        /// </summary>
        public string ChecksumType { get; set; } = "SHA256";
        
        /// <summary>
        /// 版本状态
        /// </summary>
        public ReleaseStatus Status { get; set; } = ReleaseStatus.Stable;
        
        /// <summary>
        /// 兼容的主机版本范围
        /// </summary>
        public string CompatibleHostVersions { get; set; } = "*";
    }
    
    /// <summary>
    /// 系统要求
    /// </summary>
    public class SystemRequirements
    {
        /// <summary>
        /// 支持的操作系统
        /// </summary>
        public List<string> SupportedOS { get; set; } = new() { "Windows" };
        
        /// <summary>
        /// 最低.NET版本
        /// </summary>
        public string MinimumDotNetVersion { get; set; } = "8.0";
        
        /// <summary>
        /// 所需内存（MB）
        /// </summary>
        public int RequiredMemoryMB { get; set; } = 64;
        
        /// <summary>
        /// 所需磁盘空间（MB）
        /// </summary>
        public int RequiredDiskSpaceMB { get; set; } = 10;
        
        /// <summary>
        /// 其他要求
        /// </summary>
        public List<string> AdditionalRequirements { get; set; } = new();
    }
    
    /// <summary>
    /// 发布状态枚举
    /// </summary>
    public enum ReleaseStatus
    {
        /// <summary>
        /// 开发版
        /// </summary>
        Development,
        
        /// <summary>
        /// Alpha版本
        /// </summary>
        Alpha,
        
        /// <summary>
        /// Beta版本
        /// </summary>
        Beta,
        
        /// <summary>
        /// 候选版本
        /// </summary>
        ReleaseCandidate,
        
        /// <summary>
        /// 稳定版
        /// </summary>
        Stable,
        
        /// <summary>
        /// 已弃用
        /// </summary>
        Deprecated
    }
    
    /// <summary>
    /// 验证状态枚举
    /// </summary>
    public enum VerificationStatus
    {
        /// <summary>
        /// 未验证
        /// </summary>
        Unverified,
        
        /// <summary>
        /// 验证中
        /// </summary>
        Verifying,
        
        /// <summary>
        /// 已验证
        /// </summary>
        Verified,
        
        /// <summary>
        /// 官方认证
        /// </summary>
        Official,
        
        /// <summary>
        /// 验证失败
        /// </summary>
        Failed,
        
        /// <summary>
        /// 已标记为恶意
        /// </summary>
        Malicious
    }
}
