using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BooTools.Core.Models;
using BooTools.Core.Repository.Interfaces;
using BooTools.Core.Repository.Models;

namespace BooTools.Core.Repository.Implementations
{
    /// <summary>
    /// HTTP插件仓库实现
    /// </summary>
    public class HttpPluginRepository : IPluginRepository
    {
        private readonly HttpClient _httpClient;
        private readonly BooTools.Core.ILogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private List<RemotePluginInfo> _cachedPlugins = new();
        private DateTime _lastSyncTime = DateTime.MinValue;
        private bool _disposed = false;
        
        /// <summary>
        /// 仓库信息
        /// </summary>
        public PluginRepositoryInfo RepositoryInfo { get; private set; }
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; } = false;
        
        /// <summary>
        /// 同步状态变化事件
        /// </summary>
        public event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;
        
        /// <summary>
        /// 初始化HTTP插件仓库
        /// </summary>
        /// <param name="repositoryInfo">仓库信息</param>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="logger">日志服务</param>
        public HttpPluginRepository(
            PluginRepositoryInfo repositoryInfo, 
            HttpClient httpClient, 
            BooTools.Core.ILogger logger)
        {
            RepositoryInfo = repositoryInfo ?? throw new ArgumentNullException(nameof(repositoryInfo));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            
            ConfigureHttpClient();
        }
        
        /// <summary>
        /// 初始化仓库
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>初始化结果</returns>
        public async Task<PluginResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"正在初始化HTTP插件仓库: {RepositoryInfo.Name}");
                
                // 测试连接
                var testResult = await TestConnectionAsync(cancellationToken);
                if (!testResult.IsSuccess)
                {
                    return testResult;
                }
                
                IsInitialized = true;
                _logger.LogInfo($"HTTP插件仓库初始化成功: {RepositoryInfo.Name}");
                
                return PluginResult.Success("HTTP插件仓库初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"HTTP插件仓库初始化失败: {RepositoryInfo.Name}", ex);
                return PluginResult.Failure($"HTTP插件仓库初始化失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 同步仓库数据
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>同步结果</returns>
        public async Task<PluginResult> SyncAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                OnSyncStatusChanged(SyncStatus.Syncing, 0, "开始同步插件仓库");
                _logger.LogInfo($"开始同步HTTP插件仓库: {RepositoryInfo.Name}");
                
                // 获取仓库清单
                var manifestUrl = GetManifestUrl();
                OnSyncStatusChanged(SyncStatus.Syncing, 25, "正在获取仓库清单");
                
                var response = await _httpClient.GetAsync(manifestUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                OnSyncStatusChanged(SyncStatus.Syncing, 50, "正在解析插件数据");
                
                var repositoryManifest = JsonSerializer.Deserialize<RepositoryManifest>(jsonContent, _jsonOptions);
                if (repositoryManifest?.Plugins == null)
                {
                    throw new InvalidOperationException("仓库清单格式无效");
                }
                
                // 更新缓存的插件列表
                _cachedPlugins = repositoryManifest.Plugins.ToList();
                _lastSyncTime = DateTime.Now;
                
                // 更新仓库状态
                RepositoryInfo.LastSyncTime = _lastSyncTime;
                RepositoryInfo.Status = SyncStatus.Success;
                RepositoryInfo.UpdatedTime = DateTime.Now;
                
                OnSyncStatusChanged(SyncStatus.Success, 100, $"同步完成，共获取 {_cachedPlugins.Count} 个插件");
                _logger.LogInfo($"HTTP插件仓库同步成功: {RepositoryInfo.Name}, 插件数量: {_cachedPlugins.Count}");
                
                return PluginResult.Success($"同步成功，共获取 {_cachedPlugins.Count} 个插件");
            }
            catch (Exception ex)
            {
                RepositoryInfo.Status = SyncStatus.Failed;
                OnSyncStatusChanged(SyncStatus.Failed, 0, $"同步失败: {ex.Message}");
                _logger.LogError($"HTTP插件仓库同步失败: {RepositoryInfo.Name}", ex);
                return PluginResult.Failure($"同步失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取所有可用插件
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>插件列表</returns>
        public async Task<PluginResult<IEnumerable<RemotePluginInfo>>> GetAvailablePluginsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 如果缓存为空或过期，先同步
                if (_cachedPlugins.Count == 0 || ShouldRefreshCache())
                {
                    var syncResult = await SyncAsync(cancellationToken);
                    if (!syncResult.IsSuccess)
                    {
                        return PluginResult<IEnumerable<RemotePluginInfo>>.Failure($"同步仓库失败: {syncResult.ErrorMessage}");
                    }
                }
                
                return PluginResult<IEnumerable<RemotePluginInfo>>.Success(_cachedPlugins.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取可用插件失败: {RepositoryInfo.Name}", ex);
                return PluginResult<IEnumerable<RemotePluginInfo>>.Failure($"获取可用插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 根据ID获取插件信息
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>插件信息</returns>
        public async Task<PluginResult<RemotePluginInfo?>> GetPluginAsync(string pluginId, CancellationToken cancellationToken = default)
        {
            try
            {
                var pluginsResult = await GetAvailablePluginsAsync(cancellationToken);
                if (!pluginsResult.IsSuccess || pluginsResult.Data == null)
                {
                    return PluginResult<RemotePluginInfo?>.Failure($"获取插件列表失败: {pluginsResult.ErrorMessage}");
                }
                
                var plugin = pluginsResult.Data.FirstOrDefault(p => p.Metadata.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
                return PluginResult<RemotePluginInfo?>.Success(plugin);
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取插件信息失败: {pluginId}", ex);
                return PluginResult<RemotePluginInfo?>.Failure($"获取插件信息失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 搜索插件
        /// </summary>
        /// <param name="query">搜索关键词</param>
        /// <param name="filters">搜索过滤器</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>搜索结果</returns>
        public async Task<PluginResult<IEnumerable<RemotePluginInfo>>> SearchPluginsAsync(
            string query, 
            SearchFilters? filters = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var pluginsResult = await GetAvailablePluginsAsync(cancellationToken);
                if (!pluginsResult.IsSuccess || pluginsResult.Data == null)
                {
                    return PluginResult<IEnumerable<RemotePluginInfo>>.Failure($"获取插件列表失败: {pluginsResult.ErrorMessage}");
                }
                
                var plugins = pluginsResult.Data.AsQueryable();
                
                // 关键词搜索
                if (!string.IsNullOrEmpty(query))
                {
                    plugins = plugins.Where(p => 
                        p.Metadata.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.Metadata.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.Metadata.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        p.Metadata.Author.Contains(query, StringComparison.OrdinalIgnoreCase));
                }
                
                // 应用过滤器
                if (filters != null)
                {
                    plugins = ApplyFilters(plugins, filters);
                }
                
                // 排序
                plugins = ApplySorting(plugins, filters?.SortBy ?? SortBy.Relevance, filters?.SortDirection ?? SortDirection.Descending);
                
                // 分页
                if (filters != null)
                {
                    plugins = plugins.Skip(filters.PageIndex * filters.PageSize).Take(filters.PageSize);
                }
                
                var results = plugins.ToList();
                return PluginResult<IEnumerable<RemotePluginInfo>>.Success(results.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError($"搜索插件失败: {query}", ex);
                return PluginResult<IEnumerable<RemotePluginInfo>>.Failure($"搜索插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取插件的特定版本信息
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="version">版本号</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>版本信息</returns>
        public async Task<PluginResult<PluginVersionInfo?>> GetPluginVersionAsync(
            string pluginId, 
            Version version, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var pluginResult = await GetPluginAsync(pluginId, cancellationToken);
                if (!pluginResult.IsSuccess || pluginResult.Data == null)
                {
                    return PluginResult<PluginVersionInfo?>.Failure($"获取插件失败: {pluginResult.ErrorMessage}");
                }
                
                var versionInfo = pluginResult.Data.Versions.FirstOrDefault(v => v.Version == version);
                return PluginResult<PluginVersionInfo?>.Success(versionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取插件版本信息失败: {pluginId} v{version}", ex);
                return PluginResult<PluginVersionInfo?>.Failure($"获取插件版本信息失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取插件的所有版本
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>版本列表</returns>
        public async Task<PluginResult<IEnumerable<PluginVersionInfo>>> GetPluginVersionsAsync(
            string pluginId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var pluginResult = await GetPluginAsync(pluginId, cancellationToken);
                if (!pluginResult.IsSuccess || pluginResult.Data == null)
                {
                    return PluginResult<IEnumerable<PluginVersionInfo>>.Failure($"获取插件失败: {pluginResult.ErrorMessage}");
                }
                
                var versions = pluginResult.Data.Versions.OrderByDescending(v => v.Version).AsEnumerable();
                return PluginResult<IEnumerable<PluginVersionInfo>>.Success(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取插件版本列表失败: {pluginId}", ex);
                return PluginResult<IEnumerable<PluginVersionInfo>>.Failure($"获取插件版本列表失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 检查插件更新
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="currentVersion">当前版本</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新信息</returns>
        public async Task<PluginResult<PluginVersionInfo?>> CheckForUpdatesAsync(
            string pluginId, 
            Version currentVersion, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var versionsResult = await GetPluginVersionsAsync(pluginId, cancellationToken);
                if (!versionsResult.IsSuccess || versionsResult.Data == null)
                {
                    return PluginResult<PluginVersionInfo?>.Failure($"获取版本列表失败: {versionsResult.ErrorMessage}");
                }
                
                var latestVersion = versionsResult.Data
                    .Where(v => v.Version > currentVersion)
                    .OrderByDescending(v => v.Version)
                    .FirstOrDefault();
                
                return PluginResult<PluginVersionInfo?>.Success(latestVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError($"检查插件更新失败: {pluginId} v{currentVersion}", ex);
                return PluginResult<PluginVersionInfo?>.Failure($"检查插件更新失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 验证插件包
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="version">版本号</param>
        /// <param name="filePath">插件包文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        public async Task<PluginResult<bool>> VerifyPluginPackageAsync(
            string pluginId, 
            Version version, 
            string filePath, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var versionResult = await GetPluginVersionAsync(pluginId, version, cancellationToken);
                if (!versionResult.IsSuccess || versionResult.Data == null)
                {
                    return PluginResult<bool>.Failure($"获取版本信息失败: {versionResult.ErrorMessage}");
                }
                
                var versionInfo = versionResult.Data;
                
                // 验证文件大小
                var fileInfo = new FileInfo(filePath);
                if (versionInfo.FileSize > 0 && fileInfo.Length != versionInfo.FileSize)
                {
                    return PluginResult<bool>.Success(false, $"文件大小不匹配：期望 {versionInfo.FileSize} 字节，实际 {fileInfo.Length} 字节");
                }
                
                // 验证校验和
                if (!string.IsNullOrEmpty(versionInfo.Checksum))
                {
                    var actualChecksum = await ComputeFileChecksumAsync(filePath, versionInfo.ChecksumType, cancellationToken);
                    if (!actualChecksum.Equals(versionInfo.Checksum, StringComparison.OrdinalIgnoreCase))
                    {
                        return PluginResult<bool>.Success(false, $"校验和不匹配：期望 {versionInfo.Checksum}，实际 {actualChecksum}");
                    }
                }
                
                return PluginResult<bool>.Success(true, "插件包验证通过");
            }
            catch (Exception ex)
            {
                _logger.LogError($"验证插件包失败: {pluginId} v{version}", ex);
                return PluginResult<bool>.Failure($"验证插件包失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 配置HTTP客户端
        /// </summary>
        private void ConfigureHttpClient()
        {
            // 设置用户代理
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BooTools-PluginManager/1.0");
            
            // 设置超时
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            
            // 配置认证
            if (RepositoryInfo.Authentication != null)
            {
                switch (RepositoryInfo.Authentication.Type)
                {
                    case AuthenticationType.Bearer:
                        if (!string.IsNullOrEmpty(RepositoryInfo.Authentication.Token))
                        {
                            _httpClient.DefaultRequestHeaders.Authorization = 
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", RepositoryInfo.Authentication.Token);
                        }
                        break;
                        
                    case AuthenticationType.ApiKey:
                        if (!string.IsNullOrEmpty(RepositoryInfo.Authentication.ApiKey))
                        {
                            _httpClient.DefaultRequestHeaders.Add("X-API-Key", RepositoryInfo.Authentication.ApiKey);
                        }
                        break;
                }
            }
        }
        
        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>测试结果</returns>
        private async Task<PluginResult> TestConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(RepositoryInfo.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                return PluginResult.Success("连接测试成功");
            }
            catch (Exception ex)
            {
                return PluginResult.Failure($"连接测试失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取清单URL
        /// </summary>
        /// <returns>清单URL</returns>
        private string GetManifestUrl()
        {
            var baseUrl = RepositoryInfo.Url.TrimEnd('/');
            return $"{baseUrl}/manifest.json";
        }
        
        /// <summary>
        /// 判断是否应该刷新缓存
        /// </summary>
        /// <returns>是否需要刷新</returns>
        private bool ShouldRefreshCache()
        {
            var cacheExpiry = TimeSpan.FromHours(1); // 缓存1小时
            return DateTime.Now - _lastSyncTime > cacheExpiry;
        }
        
        /// <summary>
        /// 应用搜索过滤器
        /// </summary>
        /// <param name="plugins">插件查询</param>
        /// <param name="filters">过滤器</param>
        /// <returns>过滤后的查询</returns>
        private IQueryable<RemotePluginInfo> ApplyFilters(IQueryable<RemotePluginInfo> plugins, SearchFilters filters)
        {
            if (filters.Categories.Count > 0)
            {
                plugins = plugins.Where(p => filters.Categories.Contains(p.Metadata.Category));
            }
            
            if (filters.Tags.Count > 0)
            {
                plugins = plugins.Where(p => p.Metadata.Tags.Any(t => filters.Tags.Contains(t)));
            }
            
            if (!string.IsNullOrEmpty(filters.Author))
            {
                plugins = plugins.Where(p => p.Metadata.Author.Contains(filters.Author, StringComparison.OrdinalIgnoreCase));
            }
            
            if (filters.MinRating.HasValue)
            {
                plugins = plugins.Where(p => p.Rating.AverageRating >= filters.MinRating.Value);
            }
            
            if (filters.ReleaseStatuses.Count > 0)
            {
                plugins = plugins.Where(p => filters.ReleaseStatuses.Contains(p.Status));
            }
            
            if (filters.VerificationStatuses.Count > 0)
            {
                plugins = plugins.Where(p => filters.VerificationStatuses.Contains(p.Verification));
            }
            
            if (!filters.IncludePaidPlugins)
            {
                plugins = plugins.Where(p => !p.IsPaid);
            }
            
            return plugins;
        }
        
        /// <summary>
        /// 应用排序
        /// </summary>
        /// <param name="plugins">插件查询</param>
        /// <param name="sortBy">排序方式</param>
        /// <param name="sortDirection">排序方向</param>
        /// <returns>排序后的查询</returns>
        private IQueryable<RemotePluginInfo> ApplySorting(IQueryable<RemotePluginInfo> plugins, SortBy sortBy, SortDirection sortDirection)
        {
            return sortBy switch
            {
                SortBy.Name => sortDirection == SortDirection.Ascending 
                    ? plugins.OrderBy(p => p.Metadata.Name) 
                    : plugins.OrderByDescending(p => p.Metadata.Name),
                    
                SortBy.Downloads => sortDirection == SortDirection.Ascending 
                    ? plugins.OrderBy(p => p.Downloads.TotalDownloads) 
                    : plugins.OrderByDescending(p => p.Downloads.TotalDownloads),
                    
                SortBy.Rating => sortDirection == SortDirection.Ascending 
                    ? plugins.OrderBy(p => p.Rating.AverageRating) 
                    : plugins.OrderByDescending(p => p.Rating.AverageRating),
                    
                SortBy.LastUpdated => sortDirection == SortDirection.Ascending 
                    ? plugins.OrderBy(p => p.LastUpdated) 
                    : plugins.OrderByDescending(p => p.LastUpdated),
                    
                SortBy.PublishDate => sortDirection == SortDirection.Ascending 
                    ? plugins.OrderBy(p => p.Metadata.PublishTime) 
                    : plugins.OrderByDescending(p => p.Metadata.PublishTime),
                    
                _ => plugins.OrderByDescending(p => p.Downloads.TotalDownloads) // 默认按下载量排序
            };
        }
        
        /// <summary>
        /// 计算文件校验和
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="checksumType">校验和类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>校验和</returns>
        private async Task<string> ComputeFileChecksumAsync(string filePath, string checksumType, CancellationToken cancellationToken)
        {
            using var stream = File.OpenRead(filePath);
            
            HashAlgorithm hashAlgorithm = checksumType.ToUpperInvariant() switch
            {
                "MD5" => MD5.Create(),
                "SHA1" => SHA1.Create(),
                "SHA256" => SHA256.Create(),
                "SHA512" => SHA512.Create(),
                _ => SHA256.Create()
            };
            
            using (hashAlgorithm)
            {
                var hashBytes = await hashAlgorithm.ComputeHashAsync(stream, cancellationToken);
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }
        
        /// <summary>
        /// 触发同步状态变化事件
        /// </summary>
        /// <param name="newStatus">新状态</param>
        /// <param name="progress">进度</param>
        /// <param name="message">消息</param>
        private void OnSyncStatusChanged(SyncStatus newStatus, int progress, string? message = null)
        {
            var oldStatus = RepositoryInfo.Status;
            RepositoryInfo.Status = newStatus;
            
            SyncStatusChanged?.Invoke(this, new SyncStatusChangedEventArgs(
                RepositoryInfo.Id, oldStatus, newStatus, progress, message));
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
    
    /// <summary>
    /// 仓库清单格式
    /// </summary>
    internal class RepositoryManifest
    {
        /// <summary>
        /// 仓库版本
        /// </summary>
        public string Version { get; set; } = "1.0";
        
        /// <summary>
        /// 仓库信息
        /// </summary>
        public RepositoryMetadata? Repository { get; set; }
        
        /// <summary>
        /// 插件列表
        /// </summary>
        public List<RemotePluginInfo> Plugins { get; set; } = new();
        
        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 仓库元数据
    /// </summary>
    internal class RepositoryMetadata
    {
        /// <summary>
        /// 仓库名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 仓库描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 维护者
        /// </summary>
        public string Maintainer { get; set; } = string.Empty;
        
        /// <summary>
        /// 联系邮箱
        /// </summary>
        public string Contact { get; set; } = string.Empty;
        
        /// <summary>
        /// 主页URL
        /// </summary>
        public string Homepage { get; set; } = string.Empty;
    }
}
