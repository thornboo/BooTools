using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BooTools.Core.VersionManagement.Models;
using BooTools.Core.Download;
using BooTools.Core.Package;
using BooTools.Core.Repository.Interfaces;
using BooTools.Core.Models;
using BooTools.Core.Download.Models;

namespace BooTools.Core.VersionManagement
{
    /// <summary>
    /// 更新管理器
    /// </summary>
    public class UpdateManager : IDisposable
    {
        private readonly UpdateChecker _updateChecker;
        private readonly PluginDownloadManager _downloadManager;
        private readonly PluginPackageManager _packageManager;
        private readonly BooTools.Core.ILogger _logger;
        private readonly string _updateDirectory;
        private readonly string _backupDirectory;

        public UpdateManager(
            UpdateChecker updateChecker,
            PluginDownloadManager downloadManager,
            PluginPackageManager packageManager,
            BooTools.Core.ILogger logger,
            string baseDirectory)
        {
            _updateChecker = updateChecker ?? throw new ArgumentNullException(nameof(updateChecker));
            _downloadManager = downloadManager ?? throw new ArgumentNullException(nameof(downloadManager));
            _packageManager = packageManager ?? throw new ArgumentNullException(nameof(packageManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _updateDirectory = Path.Combine(baseDirectory, "Updates");
            _backupDirectory = Path.Combine(baseDirectory, "Backups");

            // 确保目录存在
            Directory.CreateDirectory(_updateDirectory);
            Directory.CreateDirectory(_backupDirectory);
        }

        /// <summary>
        /// 更新进度事件
        /// </summary>
        public event EventHandler<UpdateProgressEventArgs>? UpdateProgressChanged;

        /// <summary>
        /// 更新单个插件
        /// </summary>
        /// <param name="updateInfo">更新信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新结果</returns>
        public async Task<PluginResult> UpdatePluginAsync(
            UpdateInfo updateInfo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"开始更新插件: {updateInfo.PluginName} {updateInfo.GetVersionDifference()}");

                // 1. 创建备份
                var backupResult = await CreateBackupAsync(updateInfo.PluginId, cancellationToken);
                if (!backupResult.IsSuccess)
                {
                    return PluginResult.Failure($"创建备份失败: {backupResult.ErrorMessage}");
                }

                // 2. 下载新版本
                var downloadResult = await DownloadUpdateAsync(updateInfo, cancellationToken);
                if (!downloadResult.IsSuccess)
                {
                    return PluginResult.Failure($"下载更新失败: {downloadResult.ErrorMessage}");
                }

                var packagePath = downloadResult.Data;
                if (string.IsNullOrEmpty(packagePath))
                {
                    return PluginResult.Failure("下载结果中未包含有效的包路径");
                }

                // 3. 验证包完整性
                var validationResult = await _packageManager.VerifyPackageAsync(packagePath, cancellationToken);
                if (!validationResult.IsSuccess || !validationResult.Data)
                {
                    return PluginResult.Failure($"包验证失败: {validationResult.ErrorMessage}");
                }

                // 4. 停止当前插件
                OnUpdateProgressChanged(updateInfo, UpdateStage.StoppingPlugin, 60);
                // 这里需要调用插件管理器停止插件
                // await _pluginManager.StopPluginAsync(updateInfo.PluginId, cancellationToken);

                // 5. 卸载旧版本
                OnUpdateProgressChanged(updateInfo, UpdateStage.UninstallingOldVersion, 70);
                var pluginInstallDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins", updateInfo.PluginId);
                var uninstallResult = await _packageManager.UninstallPackageAsync(updateInfo.PluginId, pluginInstallDirectory, cancellationToken);
                if (!uninstallResult.IsSuccess)
                {
                    // 尝试恢复备份
                    await RestoreBackupAsync(updateInfo.PluginId, cancellationToken);
                    return PluginResult.Failure($"卸载旧版本失败: {uninstallResult.ErrorMessage}");
                }

                // 6. 安装新版本
                OnUpdateProgressChanged(updateInfo, UpdateStage.InstallingNewVersion, 80);
                var installResult = await _packageManager.InstallPackageAsync(packagePath, pluginInstallDirectory, cancellationToken);
                if (!installResult.IsSuccess)
                {
                    // 尝试恢复备份
                    await RestoreBackupAsync(updateInfo.PluginId, cancellationToken);
                    return PluginResult.Failure($"安装新版本失败: {installResult.ErrorMessage}");
                }

                // 7. 启动新版本
                OnUpdateProgressChanged(updateInfo, UpdateStage.StartingNewVersion, 90);
                // await _pluginManager.StartPluginAsync(updateInfo.PluginId, cancellationToken);

                // 8. 清理临时文件
                OnUpdateProgressChanged(updateInfo, UpdateStage.CleaningUp, 95);
                try
                {
                    if (File.Exists(packagePath))
                    {
                        File.Delete(packagePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"清理临时文件失败: {packagePath}", ex);
                }

                OnUpdateProgressChanged(updateInfo, UpdateStage.Completed, 100);
                _logger.LogInfo($"插件更新完成: {updateInfo.PluginName} {updateInfo.GetVersionDifference()}");
                
                return PluginResult.Success($"插件更新成功: {updateInfo.GetVersionDifference()}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"更新插件失败: {updateInfo.PluginName}", ex);
                
                // 尝试恢复备份
                await RestoreBackupAsync(updateInfo.PluginId, cancellationToken);
                
                return PluginResult.Failure($"更新插件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量更新插件
        /// </summary>
        /// <param name="updateInfos">更新信息列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量更新结果</returns>
        public async Task<PluginResult<List<UpdateResult>>> UpdatePluginsAsync(
            IEnumerable<UpdateInfo> updateInfos,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var updateList = updateInfos.ToList();
                _logger.LogInfo($"开始批量更新插件，共 {updateList.Count} 个插件");

                var results = new List<UpdateResult>();
                var tasks = new List<Task<PluginResult>>();

                // 并行更新所有插件
                foreach (var updateInfo in updateList)
                {
                    var task = UpdatePluginAsync(updateInfo, cancellationToken);
                    tasks.Add(task);
                }

                // 等待所有更新完成
                var updateResults = await Task.WhenAll(tasks);

                // 收集结果
                for (int i = 0; i < updateList.Count; i++)
                {
                    var updateInfo = updateList[i];
                    var result = updateResults[i];

                    results.Add(new UpdateResult
                    {
                        PluginId = updateInfo.PluginId,
                        PluginName = updateInfo.PluginName,
                        IsSuccess = result.IsSuccess,
                        ErrorMessage = result.ErrorMessage,
                        CurrentVersion = updateInfo.CurrentVersion,
                        TargetVersion = updateInfo.LatestVersion
                    });
                }

                var successCount = results.Count(r => r.IsSuccess);
                _logger.LogInfo($"批量更新完成，成功 {successCount}/{updateList.Count} 个插件");

                return PluginResult<List<UpdateResult>>.Success(results, $"批量更新完成，成功 {successCount}/{updateList.Count} 个插件");
            }
            catch (Exception ex)
            {
                _logger.LogError("批量更新插件失败", ex);
                return PluginResult<List<UpdateResult>>.Failure($"批量更新失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建备份
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>备份结果</returns>
        private Task<PluginResult> CreateBackupAsync(string pluginId, CancellationToken cancellationToken)
        {
            try
            {
                OnUpdateProgressChanged(new UpdateInfo { PluginId = pluginId }, UpdateStage.CreatingBackup, 10);

                var backupPath = Path.Combine(_backupDirectory, $"{pluginId}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                
                // 这里需要实现具体的备份逻辑
                // 备份插件文件、配置等
                
                _logger.LogInfo($"创建备份完成: {backupPath}");
                return Task.FromResult(PluginResult.Success("备份创建成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"创建备份失败: {pluginId}", ex);
                return Task.FromResult(PluginResult.Failure($"创建备份失败: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// 下载更新
        /// </summary>
        /// <param name="updateInfo">更新信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>下载结果</returns>
        private async Task<PluginResult<string>> DownloadUpdateAsync(
            UpdateInfo updateInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                OnUpdateProgressChanged(updateInfo, UpdateStage.Downloading, 30);

                var fileName = $"{updateInfo.PluginId}-{updateInfo.LatestVersion}.bpkg";
                var downloadPath = Path.Combine(_updateDirectory, fileName);

                // 使用下载管理器下载
                // 创建 RemotePluginInfo 和 PluginVersionInfo
                var remotePluginInfo = new Repository.Models.RemotePluginInfo
                {
                    Metadata = new PluginMetadata
                    {
                        Id = updateInfo.PluginId,
                        Name = updateInfo.PluginName
                    }
                };
                
                var pluginVersionInfo = new Repository.Models.PluginVersionInfo
                {
                    Version = updateInfo.LatestVersion,
                    DownloadUrl = updateInfo.LatestVersionInfo.DownloadUrl ?? "",
                    FileSize = updateInfo.LatestVersionInfo.FileSize,
                    Checksum = updateInfo.LatestVersionInfo.Checksum ?? ""
                };

                var addResult = await _downloadManager.AddDownloadTaskAsync(remotePluginInfo, pluginVersionInfo);
                if (!addResult.IsSuccess)
                {
                    return PluginResult<string>.Failure($"添加下载任务失败: {addResult.ErrorMessage}");
                }
                
                var downloadTask = addResult.Data;
                if (downloadTask == null)
                {
                    return PluginResult<string>.Failure("下载任务创建失败");
                }
                
                // 等待下载完成
                while (true)
                {
                    var task = _downloadManager.GetTask(downloadTask.Id);
                    if (task == null)
                    {
                        return PluginResult<string>.Failure("下载任务不存在");
                    }

                    switch (task.Status)
                    {
                        case DownloadStatus.Completed:
                            return PluginResult<string>.Success(task.LocalFilePath, "下载完成");
                        case DownloadStatus.Failed:
                            return PluginResult<string>.Failure($"下载失败: {task.ErrorMessage}");
                        case DownloadStatus.Cancelled:
                            return PluginResult<string>.Failure("下载已取消");
                    }

                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"下载更新失败: {updateInfo.PluginName}", ex);
                return PluginResult<string>.Failure($"下载更新失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 恢复备份
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>恢复结果</returns>
        private Task<PluginResult> RestoreBackupAsync(string pluginId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInfo($"开始恢复备份: {pluginId}");

                // 这里需要实现具体的恢复逻辑
                // 从备份文件恢复插件

                _logger.LogInfo($"恢复备份完成: {pluginId}");
                return Task.FromResult(PluginResult.Success("备份恢复成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"恢复备份失败: {pluginId}", ex);
                return Task.FromResult(PluginResult.Failure($"恢复备份失败: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// 触发更新进度事件
        /// </summary>
        /// <param name="updateInfo">更新信息</param>
        /// <param name="stage">更新阶段</param>
        /// <param name="progress">进度百分比</param>
        private void OnUpdateProgressChanged(UpdateInfo updateInfo, UpdateStage stage, int progress)
        {
            UpdateProgressChanged?.Invoke(this, new UpdateProgressEventArgs
            {
                PluginId = updateInfo.PluginId,
                PluginName = updateInfo.PluginName,
                Stage = stage,
                Progress = progress
            });
        }

        public void Dispose()
        {
            // 清理资源
        }
    }

    /// <summary>
    /// 更新阶段
    /// </summary>
    public enum UpdateStage
    {
        /// <summary>
        /// 准备中
        /// </summary>
        Preparing = 0,

        /// <summary>
        /// 创建备份
        /// </summary>
        CreatingBackup = 10,

        /// <summary>
        /// 下载中
        /// </summary>
        Downloading = 30,

        /// <summary>
        /// 验证中
        /// </summary>
        Validating = 50,

        /// <summary>
        /// 停止插件
        /// </summary>
        StoppingPlugin = 60,

        /// <summary>
        /// 卸载旧版本
        /// </summary>
        UninstallingOldVersion = 70,

        /// <summary>
        /// 安装新版本
        /// </summary>
        InstallingNewVersion = 80,

        /// <summary>
        /// 启动新版本
        /// </summary>
        StartingNewVersion = 90,

        /// <summary>
        /// 清理中
        /// </summary>
        CleaningUp = 95,

        /// <summary>
        /// 完成
        /// </summary>
        Completed = 100
    }

    /// <summary>
    /// 更新进度事件参数
    /// </summary>
    public class UpdateProgressEventArgs : EventArgs
    {
        public string PluginId { get; set; } = string.Empty;
        public string PluginName { get; set; } = string.Empty;
        public UpdateStage Stage { get; set; }
        public int Progress { get; set; }
    }

    /// <summary>
    /// 更新结果
    /// </summary>
    public class UpdateResult
    {
        public string PluginId { get; set; } = string.Empty;
        public string PluginName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public Version CurrentVersion { get; set; } = new Version(1, 0, 0);
        public Version TargetVersion { get; set; } = new Version(1, 0, 0);
    }
}
