using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BooTools.Core.Models;
using BooTools.Core.Repository.Implementations;
using BooTools.Core.Repository.Interfaces;
using BooTools.Core.Repository.Models;

namespace BooTools.Core.Repository.Management
{
    /// <summary>
    /// 插件仓库管理器
    /// </summary>
    public class PluginRepositoryManager : IDisposable
    {
        private readonly BooTools.Core.ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _configDirectory;
        private readonly ConcurrentDictionary<string, IPluginRepository> _repositories;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;
        
        /// <summary>
        /// 仓库状态变化事件
        /// </summary>
        public event EventHandler<RepositoryStatusChangedEventArgs>? RepositoryStatusChanged;
        
        /// <summary>
        /// 初始化插件仓库管理器
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="configDirectory">配置目录</param>
        public PluginRepositoryManager(BooTools.Core.ILogger logger, HttpClient httpClient, string configDirectory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configDirectory = configDirectory ?? throw new ArgumentNullException(nameof(configDirectory));
            
            _repositories = new ConcurrentDictionary<string, IPluginRepository>();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            Directory.CreateDirectory(_configDirectory);
        }
        
        /// <summary>
        /// 初始化仓库管理器
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>初始化结果</returns>
        public async Task<PluginResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo("正在初始化插件仓库管理器...");
                
                // 加载已保存的仓库配置
                await LoadRepositoryConfigurationsAsync(cancellationToken);
                
                // 如果没有任何仓库，添加默认的官方仓库
                if (_repositories.IsEmpty)
                {
                    await AddDefaultRepositoriesAsync(cancellationToken);
                }
                
                _logger.LogInfo($"插件仓库管理器初始化完成，共加载 {_repositories.Count} 个仓库");
                return PluginResult.Success($"初始化完成，共加载 {_repositories.Count} 个仓库");
            }
            catch (Exception ex)
            {
                _logger.LogError("插件仓库管理器初始化失败", ex);
                return PluginResult.Failure($"初始化失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 添加仓库
        /// </summary>
        /// <param name="repositoryInfo">仓库信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>添加结果</returns>
        public async Task<PluginResult> AddRepositoryAsync(PluginRepositoryInfo repositoryInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_repositories.ContainsKey(repositoryInfo.Id))
                {
                    return PluginResult.Failure($"仓库已存在: {repositoryInfo.Id}");
                }
                
                _logger.LogInfo($"正在添加插件仓库: {repositoryInfo.Name}");
                
                // 创建仓库实例
                var repository = CreateRepository(repositoryInfo);
                
                // 初始化仓库
                var initResult = await repository.InitializeAsync(cancellationToken);
                if (!initResult.IsSuccess)
                {
                    repository.Dispose();
                    return PluginResult.Failure($"仓库初始化失败: {initResult.ErrorMessage}");
                }
                
                // 订阅仓库事件
                repository.SyncStatusChanged += OnRepositorySyncStatusChanged;
                
                // 添加到管理器
                _repositories[repositoryInfo.Id] = repository;
                
                // 保存配置
                await SaveRepositoryConfigurationAsync(repositoryInfo, cancellationToken);
                
                // 触发事件
                OnRepositoryStatusChanged(repositoryInfo.Id, RepositoryOperationType.Added, "仓库添加成功");
                
                _logger.LogInfo($"插件仓库添加成功: {repositoryInfo.Name}");
                return PluginResult.Success("仓库添加成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"添加插件仓库失败: {repositoryInfo.Name}", ex);
                return PluginResult.Failure($"添加仓库失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 移除仓库
        /// </summary>
        /// <param name="repositoryId">仓库ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>移除结果</returns>
        public async Task<PluginResult> RemoveRepositoryAsync(string repositoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_repositories.TryRemove(repositoryId, out var repository))
                {
                    return PluginResult.Failure($"仓库不存在: {repositoryId}");
                }
                
                _logger.LogInfo($"正在移除插件仓库: {repositoryId}");
                
                // 取消订阅事件
                repository.SyncStatusChanged -= OnRepositorySyncStatusChanged;
                
                // 释放资源
                repository.Dispose();
                
                // 删除配置文件
                await DeleteRepositoryConfigurationAsync(repositoryId, cancellationToken);
                
                // 触发事件
                OnRepositoryStatusChanged(repositoryId, RepositoryOperationType.Removed, "仓库移除成功");
                
                _logger.LogInfo($"插件仓库移除成功: {repositoryId}");
                return PluginResult.Success("仓库移除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"移除插件仓库失败: {repositoryId}", ex);
                return PluginResult.Failure($"移除仓库失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取所有仓库
        /// </summary>
        /// <returns>仓库列表</returns>
        public IEnumerable<IPluginRepository> GetAllRepositories()
        {
            return _repositories.Values.ToArray();
        }
        
        /// <summary>
        /// 获取启用的仓库
        /// </summary>
        /// <returns>启用的仓库列表</returns>
        public IEnumerable<IPluginRepository> GetEnabledRepositories()
        {
            return _repositories.Values.Where(r => r.RepositoryInfo.IsEnabled).ToArray();
        }
        
        /// <summary>
        /// 根据ID获取仓库
        /// </summary>
        /// <param name="repositoryId">仓库ID</param>
        /// <returns>仓库实例</returns>
        public IPluginRepository? GetRepository(string repositoryId)
        {
            return _repositories.TryGetValue(repositoryId, out var repository) ? repository : null;
        }
        
        /// <summary>
        /// 同步所有启用的仓库
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>同步结果</returns>
        public async Task<PluginResult> SyncAllRepositoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo("开始同步所有启用的插件仓库...");
                
                var enabledRepositories = GetEnabledRepositories().ToArray();
                var syncTasks = enabledRepositories.Select(repo => repo.SyncAsync(cancellationToken));
                
                var results = await Task.WhenAll(syncTasks);
                var successCount = results.Count(r => r.IsSuccess);
                var failureCount = results.Length - successCount;
                
                var message = $"仓库同步完成: 成功 {successCount} 个，失败 {failureCount} 个";
                _logger.LogInfo(message);
                
                if (failureCount == 0)
                {
                    return PluginResult.Success(message);
                }
                else
                {
                    var failureMessages = results.Where(r => !r.IsSuccess).Select(r => r.ErrorMessage);
                    return PluginResult.Success(message, string.Join("; ", failureMessages));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("同步所有仓库失败", ex);
                return PluginResult.Failure($"同步所有仓库失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 搜索所有仓库中的插件
        /// </summary>
        /// <param name="query">搜索关键词</param>
        /// <param name="filters">搜索过滤器</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>搜索结果</returns>
        public async Task<PluginResult<IEnumerable<RemotePluginInfo>>> SearchAllRepositoriesAsync(
            string query, 
            SearchFilters? filters = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var enabledRepositories = GetEnabledRepositories().ToArray();
                var searchTasks = enabledRepositories.Select(repo => repo.SearchPluginsAsync(query, filters, cancellationToken));
                
                var results = await Task.WhenAll(searchTasks);
                var allPlugins = results
                    .Where(r => r.IsSuccess && r.Data != null)
                    .SelectMany(r => r.Data!)
                    .ToList();
                
                // 去重（基于插件ID，优先级高的仓库优先）
                var uniquePlugins = allPlugins
                    .GroupBy(p => p.Metadata.Id)
                    .Select(g => g.OrderBy(p => _repositories.Values
                        .First(r => r.RepositoryInfo.Id == p.RepositoryId).RepositoryInfo.Priority)
                        .First())
                    .ToList();
                
                return PluginResult<IEnumerable<RemotePluginInfo>>.Success(uniquePlugins.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError($"搜索所有仓库失败: {query}", ex);
                return PluginResult<IEnumerable<RemotePluginInfo>>.Failure($"搜索失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 在所有仓库中查找插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>插件信息</returns>
        public async Task<PluginResult<RemotePluginInfo?>> FindPluginAsync(string pluginId, CancellationToken cancellationToken = default)
        {
            try
            {
                var enabledRepositories = GetEnabledRepositories().OrderBy(r => r.RepositoryInfo.Priority).ToArray();
                
                foreach (var repository in enabledRepositories)
                {
                    var result = await repository.GetPluginAsync(pluginId, cancellationToken);
                    if (result.IsSuccess && result.Data != null)
                    {
                        return result;
                    }
                }
                
                return PluginResult<RemotePluginInfo?>.Success(null, "未找到指定的插件");
            }
            catch (Exception ex)
            {
                _logger.LogError($"查找插件失败: {pluginId}", ex);
                return PluginResult<RemotePluginInfo?>.Failure($"查找插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 加载仓库配置
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task LoadRepositoryConfigurationsAsync(CancellationToken cancellationToken)
        {
            var configFiles = Directory.GetFiles(_configDirectory, "repository_*.json");
            
            foreach (var configFile in configFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(configFile, cancellationToken);
                    var repositoryInfo = JsonSerializer.Deserialize<PluginRepositoryInfo>(json, _jsonOptions);
                    
                    if (repositoryInfo != null)
                    {
                        var repository = CreateRepository(repositoryInfo);
                        repository.SyncStatusChanged += OnRepositorySyncStatusChanged;
                        
                        // 初始化仓库（不阻塞）
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await repository.InitializeAsync(cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"仓库初始化失败: {repositoryInfo.Name}", ex);
                            }
                        }, cancellationToken);
                        
                        _repositories[repositoryInfo.Id] = repository;
                        _logger.LogInfo($"加载仓库配置: {repositoryInfo.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"加载仓库配置失败: {configFile}", ex);
                }
            }
        }
        
        /// <summary>
        /// 添加默认仓库
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task AddDefaultRepositoriesAsync(CancellationToken cancellationToken)
        {
            var officialRepository = new PluginRepositoryInfo
            {
                Id = "official",
                Name = "BooTools 官方仓库",
                Description = "BooTools 官方维护的插件仓库",
                Url = "https://plugins.bootools.org",
                Type = RepositoryType.Http,
                IsEnabled = true,
                IsOfficial = true,
                Priority = 1
            };
            
            await AddRepositoryAsync(officialRepository, cancellationToken);
        }
        
        /// <summary>
        /// 创建仓库实例
        /// </summary>
        /// <param name="repositoryInfo">仓库信息</param>
        /// <returns>仓库实例</returns>
        private IPluginRepository CreateRepository(PluginRepositoryInfo repositoryInfo)
        {
            return repositoryInfo.Type switch
            {
                RepositoryType.Http or RepositoryType.GitHub => new HttpPluginRepository(repositoryInfo, _httpClient, _logger),
                _ => throw new NotSupportedException($"不支持的仓库类型: {repositoryInfo.Type}")
            };
        }
        
        /// <summary>
        /// 保存仓库配置
        /// </summary>
        /// <param name="repositoryInfo">仓库信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task SaveRepositoryConfigurationAsync(PluginRepositoryInfo repositoryInfo, CancellationToken cancellationToken)
        {
            var configPath = Path.Combine(_configDirectory, $"repository_{repositoryInfo.Id}.json");
            var json = JsonSerializer.Serialize(repositoryInfo, _jsonOptions);
            await File.WriteAllTextAsync(configPath, json, cancellationToken);
        }
        
        /// <summary>
        /// 删除仓库配置
        /// </summary>
        /// <param name="repositoryId">仓库ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task DeleteRepositoryConfigurationAsync(string repositoryId, CancellationToken cancellationToken)
        {
            var configPath = Path.Combine(_configDirectory, $"repository_{repositoryId}.json");
            if (File.Exists(configPath))
            {
                await Task.Run(() => File.Delete(configPath), cancellationToken);
            }
        }
        
        /// <summary>
        /// 处理仓库同步状态变化
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void OnRepositorySyncStatusChanged(object? sender, SyncStatusChangedEventArgs e)
        {
            _logger.LogInfo($"仓库同步状态变化: {e.RepositoryId} {e.OldStatus} -> {e.NewStatus}");
            OnRepositoryStatusChanged(e.RepositoryId, RepositoryOperationType.StatusChanged, e.Message);
        }
        
        /// <summary>
        /// 触发仓库状态变化事件
        /// </summary>
        /// <param name="repositoryId">仓库ID</param>
        /// <param name="operationType">操作类型</param>
        /// <param name="message">消息</param>
        private void OnRepositoryStatusChanged(string repositoryId, RepositoryOperationType operationType, string? message)
        {
            RepositoryStatusChanged?.Invoke(this, new RepositoryStatusChangedEventArgs(repositoryId, operationType, message));
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var repository in _repositories.Values)
                {
                    try
                    {
                        repository.SyncStatusChanged -= OnRepositorySyncStatusChanged;
                        repository.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"释放仓库资源失败: {repository.RepositoryInfo.Id}", ex);
                    }
                }
                
                _repositories.Clear();
                _disposed = true;
            }
        }
    }
    
    /// <summary>
    /// 仓库状态变化事件参数
    /// </summary>
    public class RepositoryStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 仓库ID
        /// </summary>
        public string RepositoryId { get; }
        
        /// <summary>
        /// 操作类型
        /// </summary>
        public RepositoryOperationType OperationType { get; }
        
        /// <summary>
        /// 消息
        /// </summary>
        public string? Message { get; }
        
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// 初始化仓库状态变化事件参数
        /// </summary>
        /// <param name="repositoryId">仓库ID</param>
        /// <param name="operationType">操作类型</param>
        /// <param name="message">消息</param>
        public RepositoryStatusChangedEventArgs(string repositoryId, RepositoryOperationType operationType, string? message)
        {
            RepositoryId = repositoryId;
            OperationType = operationType;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 仓库操作类型枚举
    /// </summary>
    public enum RepositoryOperationType
    {
        /// <summary>
        /// 仓库已添加
        /// </summary>
        Added,
        
        /// <summary>
        /// 仓库已移除
        /// </summary>
        Removed,
        
        /// <summary>
        /// 状态已变化
        /// </summary>
        StatusChanged,
        
        /// <summary>
        /// 配置已更新
        /// </summary>
        ConfigurationUpdated
    }
}
