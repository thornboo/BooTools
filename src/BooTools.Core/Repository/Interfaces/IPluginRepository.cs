using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BooTools.Core.Models;
using BooTools.Core.Repository.Models;

namespace BooTools.Core.Repository.Interfaces
{
    /// <summary>
    /// 插件仓库接口
    /// </summary>
    public interface IPluginRepository : IDisposable
    {
        /// <summary>
        /// 仓库信息
        /// </summary>
        PluginRepositoryInfo RepositoryInfo { get; }
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// 初始化仓库
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>初始化结果</returns>
        Task<PluginResult> InitializeAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 同步仓库数据
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>同步结果</returns>
        Task<PluginResult> SyncAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取所有可用插件
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>插件列表</returns>
        Task<PluginResult<IEnumerable<RemotePluginInfo>>> GetAvailablePluginsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 根据ID获取插件信息
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>插件信息</returns>
        Task<PluginResult<RemotePluginInfo?>> GetPluginAsync(string pluginId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 搜索插件
        /// </summary>
        /// <param name="query">搜索关键词</param>
        /// <param name="filters">搜索过滤器</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>搜索结果</returns>
        Task<PluginResult<IEnumerable<RemotePluginInfo>>> SearchPluginsAsync(
            string query, 
            SearchFilters? filters = null, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取插件的特定版本信息
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="version">版本号</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>版本信息</returns>
        Task<PluginResult<PluginVersionInfo?>> GetPluginVersionAsync(
            string pluginId, 
            Version version, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取插件的所有版本
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>版本列表</returns>
        Task<PluginResult<IEnumerable<PluginVersionInfo>>> GetPluginVersionsAsync(
            string pluginId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 检查插件更新
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="currentVersion">当前版本</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新信息</returns>
        Task<PluginResult<PluginVersionInfo?>> CheckForUpdatesAsync(
            string pluginId, 
            Version currentVersion, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 验证插件包
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="version">版本号</param>
        /// <param name="filePath">插件包文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        Task<PluginResult<bool>> VerifyPluginPackageAsync(
            string pluginId, 
            Version version, 
            string filePath, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 同步状态变化事件
        /// </summary>
        event EventHandler<SyncStatusChangedEventArgs> SyncStatusChanged;
    }
    
    /// <summary>
    /// 搜索过滤器
    /// </summary>
    public class SearchFilters
    {
        /// <summary>
        /// 分类过滤
        /// </summary>
        public List<string> Categories { get; set; } = new();
        
        /// <summary>
        /// 标签过滤
        /// </summary>
        public List<string> Tags { get; set; } = new();
        
        /// <summary>
        /// 作者过滤
        /// </summary>
        public string? Author { get; set; }
        
        /// <summary>
        /// 最低评分
        /// </summary>
        public double? MinRating { get; set; }
        
        /// <summary>
        /// 发布状态过滤
        /// </summary>
        public List<ReleaseStatus> ReleaseStatuses { get; set; } = new();
        
        /// <summary>
        /// 验证状态过滤
        /// </summary>
        public List<VerificationStatus> VerificationStatuses { get; set; } = new();
        
        /// <summary>
        /// 是否包含付费插件
        /// </summary>
        public bool IncludePaidPlugins { get; set; } = true;
        
        /// <summary>
        /// 排序方式
        /// </summary>
        public SortBy SortBy { get; set; } = SortBy.Relevance;
        
        /// <summary>
        /// 排序方向
        /// </summary>
        public SortDirection SortDirection { get; set; } = SortDirection.Descending;
        
        /// <summary>
        /// 分页大小
        /// </summary>
        public int PageSize { get; set; } = 20;
        
        /// <summary>
        /// 页码（从0开始）
        /// </summary>
        public int PageIndex { get; set; } = 0;
    }
    
    /// <summary>
    /// 排序方式枚举
    /// </summary>
    public enum SortBy
    {
        /// <summary>
        /// 相关性
        /// </summary>
        Relevance,
        
        /// <summary>
        /// 名称
        /// </summary>
        Name,
        
        /// <summary>
        /// 下载量
        /// </summary>
        Downloads,
        
        /// <summary>
        /// 评分
        /// </summary>
        Rating,
        
        /// <summary>
        /// 更新时间
        /// </summary>
        LastUpdated,
        
        /// <summary>
        /// 发布时间
        /// </summary>
        PublishDate
    }
    
    /// <summary>
    /// 排序方向枚举
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// 升序
        /// </summary>
        Ascending,
        
        /// <summary>
        /// 降序
        /// </summary>
        Descending
    }
    
    /// <summary>
    /// 同步状态变化事件参数
    /// </summary>
    public class SyncStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 仓库ID
        /// </summary>
        public string RepositoryId { get; }
        
        /// <summary>
        /// 旧状态
        /// </summary>
        public SyncStatus OldStatus { get; }
        
        /// <summary>
        /// 新状态
        /// </summary>
        public SyncStatus NewStatus { get; }
        
        /// <summary>
        /// 同步进度（0-100）
        /// </summary>
        public int Progress { get; }
        
        /// <summary>
        /// 状态消息
        /// </summary>
        public string? Message { get; }
        
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// 初始化同步状态变化事件参数
        /// </summary>
        /// <param name="repositoryId">仓库ID</param>
        /// <param name="oldStatus">旧状态</param>
        /// <param name="newStatus">新状态</param>
        /// <param name="progress">进度</param>
        /// <param name="message">消息</param>
        public SyncStatusChangedEventArgs(
            string repositoryId, 
            SyncStatus oldStatus, 
            SyncStatus newStatus, 
            int progress = 0, 
            string? message = null)
        {
            RepositoryId = repositoryId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Progress = progress;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
}
