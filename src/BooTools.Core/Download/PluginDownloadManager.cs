using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BooTools.Core.Download.Models;
using BooTools.Core.Models;
using BooTools.Core.Repository.Interfaces;
using BooTools.Core.Repository.Models;

namespace BooTools.Core.Download
{
    /// <summary>
    /// 插件下载管理器
    /// </summary>
    public class PluginDownloadManager : IDisposable
    {
        private readonly BooTools.Core.ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _downloadDirectory;
        private readonly string _tempDirectory;
        private readonly ConcurrentDictionary<string, DownloadTask> _downloadTasks;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly System.Threading.Timer _progressUpdateTimer;
        private bool _disposed = false;
        
        /// <summary>
        /// 最大并发下载数
        /// </summary>
        public int MaxConcurrentDownloads { get; set; } = 3;
        
        /// <summary>
        /// 下载缓冲区大小
        /// </summary>
        public int BufferSize { get; set; } = 8192;
        
        /// <summary>
        /// 进度更新间隔（毫秒）
        /// </summary>
        public int ProgressUpdateInterval { get; set; } = 500;
        
        /// <summary>
        /// 下载任务状态变化事件
        /// </summary>
        public event EventHandler<DownloadTaskStatusChangedEventArgs>? TaskStatusChanged;
        
        /// <summary>
        /// 下载进度更新事件
        /// </summary>
        public event EventHandler<DownloadProgressEventArgs>? ProgressUpdated;
        
        /// <summary>
        /// 初始化插件下载管理器
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="downloadDirectory">下载目录</param>
        public PluginDownloadManager(BooTools.Core.ILogger logger, HttpClient httpClient, string downloadDirectory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _downloadDirectory = downloadDirectory ?? throw new ArgumentNullException(nameof(downloadDirectory));
            
            _tempDirectory = Path.Combine(_downloadDirectory, "temp");
            _downloadTasks = new ConcurrentDictionary<string, DownloadTask>();
            _concurrencySemaphore = new SemaphoreSlim(MaxConcurrentDownloads, MaxConcurrentDownloads);
            
            // 创建目录
            Directory.CreateDirectory(_downloadDirectory);
            Directory.CreateDirectory(_tempDirectory);
            
            // 启动进度更新定时器
            _progressUpdateTimer = new System.Threading.Timer(UpdateProgress, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(ProgressUpdateInterval));
            
            _logger.LogInfo($"插件下载管理器初始化完成，下载目录: {_downloadDirectory}");
        }
        
        /// <summary>
        /// 添加下载任务
        /// </summary>
        /// <param name="pluginInfo">插件信息</param>
        /// <param name="versionInfo">版本信息</param>
        /// <param name="priority">下载优先级</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>下载任务</returns>
        public Task<PluginResult<DownloadTask>> AddDownloadTaskAsync(
            RemotePluginInfo pluginInfo, 
            PluginVersionInfo versionInfo,
            DownloadPriority priority = DownloadPriority.Normal,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 检查是否已存在相同的下载任务
                var existingTask = _downloadTasks.Values.FirstOrDefault(t => 
                    t.PluginId == pluginInfo.Metadata.Id && 
                    t.Version.Version == versionInfo.Version &&
                    (t.Status == DownloadStatus.Pending || t.Status == DownloadStatus.Downloading));
                
                if (existingTask != null)
                {
                    return Task.FromResult(PluginResult<DownloadTask>.Success(existingTask, "下载任务已存在"));
                }
                
                // 创建下载任务
                var task = new DownloadTask
                {
                    PluginId = pluginInfo.Metadata.Id,
                    PluginName = pluginInfo.Metadata.Name,
                    Version = versionInfo,
                    DownloadUrl = versionInfo.DownloadUrl,
                    Priority = priority,
                    RepositoryId = pluginInfo.RepositoryId,
                    TotalBytes = versionInfo.FileSize,
                    CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                };
                
                // 设置文件路径
                var fileName = $"{pluginInfo.Metadata.Id}-{versionInfo.Version}.bpkg";
                task.LocalFilePath = Path.Combine(_downloadDirectory, fileName);
                task.TempFilePath = Path.Combine(_tempDirectory, fileName + ".tmp");
                
                // 添加到任务列表
                _downloadTasks[task.Id] = task;
                
                // 触发状态变化事件
                OnTaskStatusChanged(task, DownloadStatus.Pending, DownloadStatus.Pending);
                
                _logger.LogInfo($"添加下载任务: {task.PluginName} v{task.Version.Version}");
                
                // 开始下载（异步）
                _ = Task.Run(() => ProcessDownloadTaskAsync(task), cancellationToken);
                
                return Task.FromResult(PluginResult<DownloadTask>.Success(task));
            }
            catch (Exception ex)
            {
                _logger.LogError($"添加下载任务失败: {pluginInfo.Metadata.Name}", ex);
                return Task.FromResult(PluginResult<DownloadTask>.Failure($"添加下载任务失败: {ex.Message}", ex));
            }
        }
        
        /// <summary>
        /// 取消下载任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>取消结果</returns>
        public PluginResult CancelDownloadTask(string taskId)
        {
            try
            {
                if (!_downloadTasks.TryGetValue(taskId, out var task))
                {
                    return PluginResult.Failure("下载任务不存在");
                }
                
                if (!task.CanCancel())
                {
                    return PluginResult.Failure($"任务状态 {task.Status} 不允许取消");
                }
                
                task.CancellationTokenSource?.Cancel();
                task.Status = DownloadStatus.Cancelled;
                
                OnTaskStatusChanged(task, task.Status, DownloadStatus.Cancelled);
                
                _logger.LogInfo($"取消下载任务: {task.PluginName} v{task.Version.Version}");
                return PluginResult.Success("下载任务已取消");
            }
            catch (Exception ex)
            {
                _logger.LogError($"取消下载任务失败: {taskId}", ex);
                return PluginResult.Failure($"取消下载任务失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 暂停下载任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>暂停结果</returns>
        public PluginResult PauseDownloadTask(string taskId)
        {
            try
            {
                if (!_downloadTasks.TryGetValue(taskId, out var task))
                {
                    return PluginResult.Failure("下载任务不存在");
                }
                
                if (!task.CanPause())
                {
                    return PluginResult.Failure($"任务状态 {task.Status} 不允许暂停");
                }
                
                var oldStatus = task.Status;
                task.Status = DownloadStatus.Paused;
                
                OnTaskStatusChanged(task, oldStatus, DownloadStatus.Paused);
                
                _logger.LogInfo($"暂停下载任务: {task.PluginName} v{task.Version.Version}");
                return PluginResult.Success("下载任务已暂停");
            }
            catch (Exception ex)
            {
                _logger.LogError($"暂停下载任务失败: {taskId}", ex);
                return PluginResult.Failure($"暂停下载任务失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 恢复下载任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>恢复结果</returns>
        public PluginResult ResumeDownloadTask(string taskId)
        {
            try
            {
                if (!_downloadTasks.TryGetValue(taskId, out var task))
                {
                    return PluginResult.Failure("下载任务不存在");
                }
                
                if (!task.CanResume())
                {
                    return PluginResult.Failure($"任务状态 {task.Status} 不允许恢复");
                }
                
                var oldStatus = task.Status;
                task.Status = DownloadStatus.Pending;
                
                OnTaskStatusChanged(task, oldStatus, DownloadStatus.Pending);
                
                // 重新开始下载（异步）
                _ = Task.Run(() => ProcessDownloadTaskAsync(task));
                
                _logger.LogInfo($"恢复下载任务: {task.PluginName} v{task.Version.Version}");
                return PluginResult.Success("下载任务已恢复");
            }
            catch (Exception ex)
            {
                _logger.LogError($"恢复下载任务失败: {taskId}", ex);
                return PluginResult.Failure($"恢复下载任务失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 重试下载任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>重试结果</returns>
        public PluginResult RetryDownloadTask(string taskId)
        {
            try
            {
                if (!_downloadTasks.TryGetValue(taskId, out var task))
                {
                    return PluginResult.Failure("下载任务不存在");
                }
                
                if (!task.CanRetry())
                {
                    return PluginResult.Failure($"任务不允许重试，当前状态: {task.Status}，重试次数: {task.RetryCount}/{task.MaxRetries}");
                }
                
                task.RetryCount++;
                task.Status = DownloadStatus.Pending;
                task.ErrorMessage = null;
                task.CancellationTokenSource = new CancellationTokenSource();
                
                OnTaskStatusChanged(task, DownloadStatus.Failed, DownloadStatus.Pending);
                
                // 重新开始下载（异步）
                _ = Task.Run(() => ProcessDownloadTaskAsync(task));
                
                _logger.LogInfo($"重试下载任务: {task.PluginName} v{task.Version.Version} (第 {task.RetryCount} 次重试)");
                return PluginResult.Success($"开始第 {task.RetryCount} 次重试");
            }
            catch (Exception ex)
            {
                _logger.LogError($"重试下载任务失败: {taskId}", ex);
                return PluginResult.Failure($"重试下载任务失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取所有下载任务
        /// </summary>
        /// <returns>下载任务列表</returns>
        public IEnumerable<DownloadTask> GetAllTasks()
        {
            return _downloadTasks.Values.ToArray();
        }
        
        /// <summary>
        /// 获取活动的下载任务
        /// </summary>
        /// <returns>活动的下载任务列表</returns>
        public IEnumerable<DownloadTask> GetActiveTasks()
        {
            return _downloadTasks.Values.Where(t => 
                t.Status == DownloadStatus.Pending || 
                t.Status == DownloadStatus.Downloading).ToArray();
        }
        
        /// <summary>
        /// 根据ID获取下载任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>下载任务</returns>
        public DownloadTask? GetTask(string taskId)
        {
            return _downloadTasks.TryGetValue(taskId, out var task) ? task : null;
        }
        
        /// <summary>
        /// 清理已完成的任务
        /// </summary>
        /// <param name="olderThan">清理多久之前的任务</param>
        /// <returns>清理结果</returns>
        public PluginResult CleanupCompletedTasks(TimeSpan? olderThan = null)
        {
            try
            {
                var cutoffTime = DateTime.Now - (olderThan ?? TimeSpan.FromDays(7));
                var tasksToRemove = _downloadTasks.Values.Where(t => 
                    (t.Status == DownloadStatus.Completed || t.Status == DownloadStatus.Failed || t.Status == DownloadStatus.Cancelled) &&
                    (t.CompletedTime ?? t.CreatedTime) < cutoffTime).ToArray();
                
                foreach (var task in tasksToRemove)
                {
                    _downloadTasks.TryRemove(task.Id, out _);
                    
                    // 清理临时文件
                    if (File.Exists(task.TempFilePath))
                    {
                        try
                        {
                            File.Delete(task.TempFilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"删除临时文件失败: {task.TempFilePath}", ex);
                        }
                    }
                }
                
                _logger.LogInfo($"清理已完成的下载任务: {tasksToRemove.Length} 个");
                return PluginResult.Success($"已清理 {tasksToRemove.Length} 个已完成的任务");
            }
            catch (Exception ex)
            {
                _logger.LogError("清理已完成任务失败", ex);
                return PluginResult.Failure($"清理失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 处理下载任务
        /// </summary>
        /// <param name="task">下载任务</param>
        private async Task ProcessDownloadTaskAsync(DownloadTask task)
        {
            await _concurrencySemaphore.WaitAsync(task.CancellationTokenSource?.Token ?? CancellationToken.None);
            
            try
            {
                if (task.CancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    task.Status = DownloadStatus.Cancelled;
                    return;
                }
                
                task.Status = DownloadStatus.Downloading;
                task.StartTime = DateTime.Now;
                OnTaskStatusChanged(task, DownloadStatus.Pending, DownloadStatus.Downloading);
                
                await DownloadFileAsync(task);
                
                if (task.Status == DownloadStatus.Downloading)
                {
                    // 验证下载的文件
                    task.Status = DownloadStatus.Verifying;
                    OnTaskStatusChanged(task, DownloadStatus.Downloading, DownloadStatus.Verifying);
                    
                    var verificationResult = await VerifyDownloadedFileAsync(task);
                    if (verificationResult.IsSuccess && verificationResult.Data == true)
                    {
                        // 移动文件到最终位置
                        if (File.Exists(task.TempFilePath))
                        {
                            File.Move(task.TempFilePath, task.LocalFilePath, true);
                        }
                        
                        task.Status = DownloadStatus.Completed;
                        task.CompletedTime = DateTime.Now;
                        task.Progress = 100;
                        
                        OnTaskStatusChanged(task, DownloadStatus.Verifying, DownloadStatus.Completed);
                        _logger.LogInfo($"下载完成: {task.PluginName} v{task.Version.Version}");
                    }
                    else
                    {
                        task.Status = DownloadStatus.VerificationFailed;
                        task.ErrorMessage = verificationResult.ErrorMessage ?? "文件验证失败";
                        OnTaskStatusChanged(task, DownloadStatus.Verifying, DownloadStatus.VerificationFailed);
                        _logger.LogError($"文件验证失败: {task.PluginName} v{task.Version.Version} - {task.ErrorMessage}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                task.Status = DownloadStatus.Cancelled;
                OnTaskStatusChanged(task, DownloadStatus.Downloading, DownloadStatus.Cancelled);
                _logger.LogInfo($"下载已取消: {task.PluginName} v{task.Version.Version}");
            }
            catch (Exception ex)
            {
                task.Status = DownloadStatus.Failed;
                task.ErrorMessage = ex.Message;
                OnTaskStatusChanged(task, DownloadStatus.Downloading, DownloadStatus.Failed);
                _logger.LogError($"下载失败: {task.PluginName} v{task.Version.Version}", ex);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }
        
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="task">下载任务</param>
        private async Task DownloadFileAsync(DownloadTask task)
        {
            var cancellationToken = task.CancellationTokenSource?.Token ?? CancellationToken.None;
            
            // 检查是否支持断点续传
            long startPosition = 0;
            if (task.SupportsResume && File.Exists(task.TempFilePath))
            {
                startPosition = new FileInfo(task.TempFilePath).Length;
                task.BytesDownloaded = startPosition;
            }
            
            using var request = new HttpRequestMessage(HttpMethod.Get, task.DownloadUrl);
            
            // 设置断点续传头
            if (startPosition > 0)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startPosition, null);
            }
            
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            // 检查是否支持断点续传
            if (startPosition > 0 && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
            {
                // 服务器不支持断点续传，重新开始下载
                startPosition = 0;
                task.BytesDownloaded = 0;
                if (File.Exists(task.TempFilePath))
                {
                    File.Delete(task.TempFilePath);
                }
            }
            
            response.EnsureSuccessStatusCode();
            
            // 获取文件总大小
            if (task.TotalBytes <= 0)
            {
                task.TotalBytes = response.Content.Headers.ContentLength ?? 0;
            }
            
            // 下载文件
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(task.TempFilePath, startPosition > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write);
            
            var buffer = new byte[BufferSize];
            int bytesRead;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                if (task.Status == DownloadStatus.Paused)
                {
                    // 等待恢复或取消
                    while (task.Status == DownloadStatus.Paused && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                task.BytesDownloaded += bytesRead;
            }
            
            await fileStream.FlushAsync(cancellationToken);
        }
        
        /// <summary>
        /// 验证下载的文件
        /// </summary>
        /// <param name="task">下载任务</param>
        /// <returns>验证结果</returns>
        private Task<PluginResult<bool>> VerifyDownloadedFileAsync(DownloadTask task)
        {
            try
            {
                var filePath = File.Exists(task.LocalFilePath) ? task.LocalFilePath : task.TempFilePath;
                if (!File.Exists(filePath))
                {
                    return Task.FromResult(PluginResult<bool>.Failure("下载的文件不存在"));
                }
                
                var fileInfo = new FileInfo(filePath);
                
                // 验证文件大小
                if (task.Version.FileSize > 0 && fileInfo.Length != task.Version.FileSize)
                {
                    return Task.FromResult(PluginResult<bool>.Success(false, $"文件大小不匹配：期望 {task.Version.FileSize} 字节，实际 {fileInfo.Length} 字节"));
                }
                
                // TODO: 验证校验和（如果提供的话）
                // 这里可以添加校验和验证逻辑
                
                return Task.FromResult(PluginResult<bool>.Success(true, "文件验证通过"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(PluginResult<bool>.Failure($"文件验证失败: {ex.Message}", ex));
            }
        }
        
        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="state">状态对象</param>
        private void UpdateProgress(object? state)
        {
            try
            {
                var activeTasks = GetActiveTasks().ToArray();
                
                foreach (var task in activeTasks)
                {
                    if (task.Status == DownloadStatus.Downloading)
                    {
                        task.UpdateStatistics();
                        OnProgressUpdated(task);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("更新下载进度失败", ex);
            }
        }
        
        /// <summary>
        /// 触发任务状态变化事件
        /// </summary>
        /// <param name="task">下载任务</param>
        /// <param name="oldStatus">旧状态</param>
        /// <param name="newStatus">新状态</param>
        private void OnTaskStatusChanged(DownloadTask task, DownloadStatus oldStatus, DownloadStatus newStatus)
        {
            TaskStatusChanged?.Invoke(this, new DownloadTaskStatusChangedEventArgs(task, oldStatus, newStatus));
        }
        
        /// <summary>
        /// 触发进度更新事件
        /// </summary>
        /// <param name="task">下载任务</param>
        private void OnProgressUpdated(DownloadTask task)
        {
            ProgressUpdated?.Invoke(this, new DownloadProgressEventArgs(task));
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _progressUpdateTimer?.Dispose();
                _concurrencySemaphore?.Dispose();
                
                // 取消所有活动的下载任务
                foreach (var task in GetActiveTasks())
                {
                    task.CancellationTokenSource?.Cancel();
                }
                
                _disposed = true;
            }
        }
    }
    
    /// <summary>
    /// 下载任务状态变化事件参数
    /// </summary>
    public class DownloadTaskStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 下载任务
        /// </summary>
        public DownloadTask Task { get; }
        
        /// <summary>
        /// 旧状态
        /// </summary>
        public DownloadStatus OldStatus { get; }
        
        /// <summary>
        /// 新状态
        /// </summary>
        public DownloadStatus NewStatus { get; }
        
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// 初始化下载任务状态变化事件参数
        /// </summary>
        /// <param name="task">下载任务</param>
        /// <param name="oldStatus">旧状态</param>
        /// <param name="newStatus">新状态</param>
        public DownloadTaskStatusChangedEventArgs(DownloadTask task, DownloadStatus oldStatus, DownloadStatus newStatus)
        {
            Task = task;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 下载进度事件参数
    /// </summary>
    public class DownloadProgressEventArgs : EventArgs
    {
        /// <summary>
        /// 下载任务
        /// </summary>
        public DownloadTask Task { get; }
        
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// 初始化下载进度事件参数
        /// </summary>
        /// <param name="task">下载任务</param>
        public DownloadProgressEventArgs(DownloadTask task)
        {
            Task = task;
            Timestamp = DateTime.Now;
        }
    }
}
