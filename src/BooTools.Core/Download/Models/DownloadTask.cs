using System;
using System.Threading;
using BooTools.Core.Repository.Models;

namespace BooTools.Core.Download.Models
{
    /// <summary>
    /// 下载任务
    /// </summary>
    public class DownloadTask
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 插件ID
        /// </summary>
        public string PluginId { get; set; } = string.Empty;
        
        /// <summary>
        /// 插件名称
        /// </summary>
        public string PluginName { get; set; } = string.Empty;
        
        /// <summary>
        /// 版本信息
        /// </summary>
        public PluginVersionInfo Version { get; set; } = new();
        
        /// <summary>
        /// 下载URL
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// 本地文件路径
        /// </summary>
        public string LocalFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// 临时文件路径
        /// </summary>
        public string TempFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// 下载状态
        /// </summary>
        public DownloadStatus Status { get; set; } = DownloadStatus.Pending;
        
        /// <summary>
        /// 下载进度（0-100）
        /// </summary>
        public int Progress { get; set; } = 0;
        
        /// <summary>
        /// 已下载字节数
        /// </summary>
        public long BytesDownloaded { get; set; } = 0;
        
        /// <summary>
        /// 总字节数
        /// </summary>
        public long TotalBytes { get; set; } = 0;
        
        /// <summary>
        /// 下载速度（字节/秒）
        /// </summary>
        public long DownloadSpeed { get; set; } = 0;
        
        /// <summary>
        /// 剩余时间（秒）
        /// </summary>
        public TimeSpan EstimatedTimeRemaining { get; set; } = TimeSpan.Zero;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedTime { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 0;
        
        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>
        /// 是否支持断点续传
        /// </summary>
        public bool SupportsResume { get; set; } = true;
        
        /// <summary>
        /// 取消令牌源
        /// </summary>
        public CancellationTokenSource? CancellationTokenSource { get; set; }
        
        /// <summary>
        /// 优先级
        /// </summary>
        public DownloadPriority Priority { get; set; } = DownloadPriority.Normal;
        
        /// <summary>
        /// 来源仓库ID
        /// </summary>
        public string RepositoryId { get; set; } = string.Empty;
        
        /// <summary>
        /// 附加数据
        /// </summary>
        public object? UserData { get; set; }
        
        /// <summary>
        /// 计算下载进度百分比
        /// </summary>
        /// <returns>进度百分比</returns>
        public int CalculateProgress()
        {
            if (TotalBytes <= 0) return 0;
            return (int)((BytesDownloaded * 100) / TotalBytes);
        }
        
        /// <summary>
        /// 计算下载速度
        /// </summary>
        /// <returns>下载速度（字节/秒）</returns>
        public long CalculateDownloadSpeed()
        {
            if (StartTime == null || BytesDownloaded <= 0) return 0;
            
            var elapsed = DateTime.Now - StartTime.Value;
            if (elapsed.TotalSeconds <= 0) return 0;
            
            return (long)(BytesDownloaded / elapsed.TotalSeconds);
        }
        
        /// <summary>
        /// 计算剩余时间
        /// </summary>
        /// <returns>剩余时间</returns>
        public TimeSpan CalculateEstimatedTimeRemaining()
        {
            if (DownloadSpeed <= 0 || TotalBytes <= 0 || BytesDownloaded >= TotalBytes)
                return TimeSpan.Zero;
            
            var remainingBytes = TotalBytes - BytesDownloaded;
            var remainingSeconds = remainingBytes / DownloadSpeed;
            
            return TimeSpan.FromSeconds(remainingSeconds);
        }
        
        /// <summary>
        /// 更新统计信息
        /// </summary>
        public void UpdateStatistics()
        {
            Progress = CalculateProgress();
            DownloadSpeed = CalculateDownloadSpeed();
            EstimatedTimeRemaining = CalculateEstimatedTimeRemaining();
        }
        
        /// <summary>
        /// 是否可以重试
        /// </summary>
        /// <returns>是否可以重试</returns>
        public bool CanRetry()
        {
            return RetryCount < MaxRetries && 
                   (Status == DownloadStatus.Failed || Status == DownloadStatus.Cancelled);
        }
        
        /// <summary>
        /// 是否可以取消
        /// </summary>
        /// <returns>是否可以取消</returns>
        public bool CanCancel()
        {
            return Status == DownloadStatus.Pending || 
                   Status == DownloadStatus.Downloading || 
                   Status == DownloadStatus.Paused;
        }
        
        /// <summary>
        /// 是否可以暂停
        /// </summary>
        /// <returns>是否可以暂停</returns>
        public bool CanPause()
        {
            return Status == DownloadStatus.Downloading;
        }
        
        /// <summary>
        /// 是否可以恢复
        /// </summary>
        /// <returns>是否可以恢复</returns>
        public bool CanResume()
        {
            return Status == DownloadStatus.Paused && SupportsResume;
        }
    }
    
    /// <summary>
    /// 下载状态枚举
    /// </summary>
    public enum DownloadStatus
    {
        /// <summary>
        /// 等待中
        /// </summary>
        Pending,
        
        /// <summary>
        /// 下载中
        /// </summary>
        Downloading,
        
        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 已失败
        /// </summary>
        Failed,
        
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled,
        
        /// <summary>
        /// 验证中
        /// </summary>
        Verifying,
        
        /// <summary>
        /// 验证失败
        /// </summary>
        VerificationFailed
    }
    
    /// <summary>
    /// 下载优先级枚举
    /// </summary>
    public enum DownloadPriority
    {
        /// <summary>
        /// 低优先级
        /// </summary>
        Low = 0,
        
        /// <summary>
        /// 普通优先级
        /// </summary>
        Normal = 1,
        
        /// <summary>
        /// 高优先级
        /// </summary>
        High = 2,
        
        /// <summary>
        /// 紧急优先级
        /// </summary>
        Urgent = 3
    }
}
