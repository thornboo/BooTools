using System;
using System.Collections.Generic;

namespace BooTools.Core.Repository.Models
{
    /// <summary>
    /// 插件仓库信息
    /// </summary>
    public class PluginRepositoryInfo
    {
        /// <summary>
        /// 仓库ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// 仓库名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 仓库描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 仓库URL
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// 仓库类型
        /// </summary>
        public RepositoryType Type { get; set; } = RepositoryType.Http;
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        
        /// <summary>
        /// 是否为官方仓库
        /// </summary>
        public bool IsOfficial { get; set; } = false;
        
        /// <summary>
        /// 优先级（数字越小优先级越高）
        /// </summary>
        public int Priority { get; set; } = 100;
        
        /// <summary>
        /// 最后同步时间
        /// </summary>
        public DateTime LastSyncTime { get; set; } = DateTime.MinValue;
        
        /// <summary>
        /// 同步状态
        /// </summary>
        public SyncStatus Status { get; set; } = SyncStatus.NotSynced;
        
        /// <summary>
        /// 认证信息
        /// </summary>
        public AuthenticationInfo? Authentication { get; set; }
        
        /// <summary>
        /// 仓库元数据
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 仓库类型枚举
    /// </summary>
    public enum RepositoryType
    {
        /// <summary>
        /// HTTP/HTTPS 仓库
        /// </summary>
        Http,
        
        /// <summary>
        /// GitHub 仓库
        /// </summary>
        GitHub,
        
        /// <summary>
        /// Git 仓库
        /// </summary>
        Git,
        
        /// <summary>
        /// 本地文件系统
        /// </summary>
        Local,
        
        /// <summary>
        /// FTP 服务器
        /// </summary>
        Ftp
    }
    
    /// <summary>
    /// 同步状态枚举
    /// </summary>
    public enum SyncStatus
    {
        /// <summary>
        /// 未同步
        /// </summary>
        NotSynced,
        
        /// <summary>
        /// 同步中
        /// </summary>
        Syncing,
        
        /// <summary>
        /// 同步成功
        /// </summary>
        Success,
        
        /// <summary>
        /// 同步失败
        /// </summary>
        Failed,
        
        /// <summary>
        /// 需要更新
        /// </summary>
        NeedsUpdate
    }
    
    /// <summary>
    /// 认证信息
    /// </summary>
    public class AuthenticationInfo
    {
        /// <summary>
        /// 认证类型
        /// </summary>
        public AuthenticationType Type { get; set; } = AuthenticationType.None;
        
        /// <summary>
        /// 用户名
        /// </summary>
        public string? Username { get; set; }
        
        /// <summary>
        /// 密码或令牌
        /// </summary>
        public string? Token { get; set; }
        
        /// <summary>
        /// API密钥
        /// </summary>
        public string? ApiKey { get; set; }
        
        /// <summary>
        /// 额外的认证参数
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new();
    }
    
    /// <summary>
    /// 认证类型枚举
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// 无需认证
        /// </summary>
        None,
        
        /// <summary>
        /// 基本认证（用户名密码）
        /// </summary>
        Basic,
        
        /// <summary>
        /// Bearer Token
        /// </summary>
        Bearer,
        
        /// <summary>
        /// API Key
        /// </summary>
        ApiKey,
        
        /// <summary>
        /// OAuth
        /// </summary>
        OAuth
    }
}
