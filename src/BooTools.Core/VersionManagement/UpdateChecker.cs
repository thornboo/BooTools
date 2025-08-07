using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BooTools.Core.VersionManagement.Models;
using BooTools.Core.Repository.Interfaces;
using BooTools.Core.Repository.Models;
using BooTools.Core.Models;

namespace BooTools.Core.VersionManagement
{
    /// <summary>
    /// 更新检查器
    /// </summary>
    public class UpdateChecker
    {
        private readonly IPluginRepository _repository;
        private readonly BooTools.Core.ILogger _logger;

        public UpdateChecker(IPluginRepository repository, BooTools.Core.ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 检查插件更新
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="currentVersion">当前版本</param>
        /// <param name="hostVersion">主机版本</param>
        /// <param name="includePrerelease">是否包含预发布版本</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新检查结果</returns>
        public async Task<PluginResult<UpdateInfo?>> CheckForUpdatesAsync(
            string pluginId,
            Version currentVersion,
            Version hostVersion,
            bool includePrerelease = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"检查插件更新: {pluginId} v{currentVersion}");

                // 获取远程插件信息
                var pluginResult = await _repository.GetPluginAsync(pluginId, cancellationToken);
                if (!pluginResult.IsSuccess)
                {
                    return PluginResult<UpdateInfo?>.Failure($"获取插件信息失败: {pluginResult.ErrorMessage}");
                }

                var remotePlugin = pluginResult.Data;
                if (remotePlugin == null)
                {
                    return PluginResult<UpdateInfo?>.Success(null, "插件不存在于远程仓库");
                }

                // 获取可用版本
                var versionsResult = await _repository.GetPluginVersionsAsync(pluginId, cancellationToken);
                if (!versionsResult.IsSuccess)
                {
                    return PluginResult<UpdateInfo?>.Failure($"获取版本信息失败: {versionsResult.ErrorMessage}");
                }

                var availableVersions = versionsResult.Data?.ToList() ?? new List<PluginVersionInfo>();

                // 过滤版本 - 转换为 VersionInfo
                var versionInfos = availableVersions.Select(v => new VersionInfo
                {
                    Version = v.Version,
                    ReleaseDate = v.ReleaseDate,
                    DownloadUrl = v.DownloadUrl,
                    FileSize = v.FileSize,
                    Checksum = v.Checksum,
                    CompatibleHostVersions = v.CompatibleHostVersions
                }).ToList();
                
                var compatibleVersions = versionInfos
                    .Where(v => IsVersionCompatible(v, hostVersion))
                    .ToList();

                if (!includePrerelease)
                {
                    compatibleVersions = compatibleVersions
                        .Where(v => string.IsNullOrEmpty(v.PrereleaseTag))
                        .ToList();
                }

                // 找到最新版本
                var latestVersion = VersionComparer.GetLatestVersion(compatibleVersions);
                if (latestVersion == null)
                {
                    return PluginResult<UpdateInfo?>.Success(null, "没有找到兼容的更新版本");
                }

                // 比较版本
                if (VersionComparer.Compare(latestVersion.Version, currentVersion) <= 0)
                {
                    return PluginResult<UpdateInfo?>.Success(null, "当前已是最新版本");
                }

                // 创建更新信息
                var updateInfo = new UpdateInfo
                {
                    PluginId = pluginId,
                    PluginName = remotePlugin.Metadata.Name,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion.Version,
                    LatestVersionInfo = latestVersion,
                    IsForcedUpdate = latestVersion.IsForcedUpdate,
                    ReleaseNotes = latestVersion.ReleaseNotes,
                    UpdateSize = latestVersion.FileSize,
                    ReleaseDate = latestVersion.ReleaseDate
                };

                _logger.LogInfo($"发现更新: {pluginId} {currentVersion} -> {latestVersion.Version}");
                return PluginResult<UpdateInfo?>.Success(updateInfo, "发现可用更新");
            }
            catch (Exception ex)
            {
                _logger.LogError($"检查更新失败: {pluginId}", ex);
                return PluginResult<UpdateInfo?>.Failure($"检查更新失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量检查插件更新
        /// </summary>
        /// <param name="plugins">插件列表</param>
        /// <param name="hostVersion">主机版本</param>
        /// <param name="includePrerelease">是否包含预发布版本</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量更新检查结果</returns>
        public async Task<PluginResult<List<UpdateInfo>>> CheckForUpdatesAsync(
            IEnumerable<InstalledPluginInfo> plugins,
            Version hostVersion,
            bool includePrerelease = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"批量检查插件更新，共 {plugins.Count()} 个插件");

                var updateResults = new List<UpdateInfo>();
                var tasks = new List<Task<PluginResult<UpdateInfo?>>>();

                // 并行检查所有插件
                foreach (var plugin in plugins)
                {
                    var task = CheckForUpdatesAsync(
                        plugin.Id,
                        plugin.Version,
                        hostVersion,
                        includePrerelease,
                        cancellationToken);
                    tasks.Add(task);
                }

                // 等待所有检查完成
                var results = await Task.WhenAll(tasks);

                // 收集有更新的插件
                foreach (var result in results)
                {
                    if (result.IsSuccess && result.Data != null)
                    {
                        updateResults.Add(result.Data);
                    }
                }

                _logger.LogInfo($"批量检查完成，发现 {updateResults.Count} 个可用更新");
                return PluginResult<List<UpdateInfo>>.Success(updateResults, $"发现 {updateResults.Count} 个可用更新");
            }
            catch (Exception ex)
            {
                _logger.LogError("批量检查更新失败", ex);
                return PluginResult<List<UpdateInfo>>.Failure($"批量检查更新失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查版本兼容性
        /// </summary>
        /// <param name="versionInfo">版本信息</param>
        /// <param name="hostVersion">主机版本</param>
        /// <returns>是否兼容</returns>
        private bool IsVersionCompatible(VersionInfo versionInfo, Version hostVersion)
        {
            if (string.IsNullOrWhiteSpace(versionInfo.CompatibleHostVersions))
                return true;

            return VersionComparer.IsCompatible(versionInfo.Version, hostVersion, versionInfo.CompatibleHostVersions);
        }

        /// <summary>
        /// 获取插件更新历史
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="currentVersion">当前版本</param>
        /// <param name="limit">限制数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新历史</returns>
        public async Task<PluginResult<List<VersionInfo>>> GetUpdateHistoryAsync(
            string pluginId,
            Version currentVersion,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"获取插件更新历史: {pluginId}");

                var versionsResult = await _repository.GetPluginVersionsAsync(pluginId, cancellationToken);
                if (!versionsResult.IsSuccess)
                {
                    return PluginResult<List<VersionInfo>>.Failure($"获取版本信息失败: {versionsResult.ErrorMessage}");
                }

                var versions = (versionsResult.Data ?? new List<PluginVersionInfo>())
                    .Where(v => VersionComparer.Compare(v.Version, currentVersion) > 0)
                    .OrderByDescending(v => v.Version)
                    .Take(limit)
                    .Select(v => new VersionInfo
                    {
                        Version = v.Version,
                        ReleaseDate = v.ReleaseDate,
                        ReleaseNotes = v.Description,
                        DownloadUrl = v.DownloadUrl,
                        FileSize = v.FileSize,
                        Checksum = v.Checksum,
                        CompatibleHostVersions = v.CompatibleHostVersions
                    })
                    .ToList();

                return PluginResult<List<VersionInfo>>.Success(versions, $"获取到 {versions.Count} 个更新版本");
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取更新历史失败: {pluginId}", ex);
                return PluginResult<List<VersionInfo>>.Failure($"获取更新历史失败: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 已安装插件信息
    /// </summary>
    public class InstalledPluginInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Version Version { get; set; } = new Version(1, 0, 0);
        public string? RepositoryId { get; set; }
    }

    /// <summary>
    /// 更新信息
    /// </summary>
    public class UpdateInfo
    {
        public string PluginId { get; set; } = string.Empty;
        public string PluginName { get; set; } = string.Empty;
        public Version CurrentVersion { get; set; } = new Version(1, 0, 0);
        public Version LatestVersion { get; set; } = new Version(1, 0, 0);
        public VersionInfo LatestVersionInfo { get; set; } = new();
        public bool IsForcedUpdate { get; set; }
        public string? ReleaseNotes { get; set; }
        public long UpdateSize { get; set; }
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// 获取版本差异描述
        /// </summary>
        /// <returns>版本差异描述</returns>
        public string GetVersionDifference()
        {
            var current = CurrentVersion.ToString();
            var latest = LatestVersion.ToString();
            return $"{current} → {latest}";
        }

        /// <summary>
        /// 获取文件大小描述
        /// </summary>
        /// <returns>文件大小描述</returns>
        public string GetFileSizeDescription()
        {
            if (UpdateSize <= 0) return "未知大小";

            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (UpdateSize >= GB)
                return $"{UpdateSize / (double)GB:F1} GB";
            if (UpdateSize >= MB)
                return $"{UpdateSize / (double)MB:F1} MB";
            if (UpdateSize >= KB)
                return $"{UpdateSize / (double)KB:F1} KB";
            
            return $"{UpdateSize} B";
        }
    }
}
