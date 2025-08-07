using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using BooTools.Core.Interfaces;
using BooTools.Core.Models;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace BooTools.Plugins.WallpaperSwitcher
{
    /// <summary>
    /// 现代版壁纸切换器插件
    /// </summary>
    public class WallpaperSwitcherModernPlugin : IPlugin
    {
        private Timer? _timer;
        private WallpaperConfig _config = new();
        private List<string> _wallpaperFiles;
        private int _currentIndex;
        private IPluginContext? _context;
        private PluginStatus _status = PluginStatus.Installed;
        
        /// <summary>
        /// 插件元数据
        /// </summary>
        public PluginMetadata Metadata { get; }
        
        /// <summary>
        /// 插件状态
        /// </summary>
        public PluginStatus Status => _status;
        
        /// <summary>
        /// 插件状态变化事件
        /// </summary>
        public event EventHandler<PluginStatusChangedEventArgs>? StatusChanged;
        
        /// <summary>
        /// 初始化现代版壁纸切换器插件
        /// </summary>
        public WallpaperSwitcherModernPlugin()
        {
            _wallpaperFiles = new List<string>();
            _currentIndex = 0;
            
            // 设置插件元数据
            Metadata = new PluginMetadata
            {
                Id = "wallpaper-switcher",
                Name = "壁纸切换器",
                Description = "定时自动切换桌面壁纸的现代化插件",
                Version = new Version(1, 0, 0),
                Author = "thornboo",
                IconPath = "icon.png",
                Category = "Desktop",
                Tags = { "wallpaper", "desktop", "automation", "modern" },
                License = "MIT",
                RequiredPermissions = { "FileSystem.Read", "Registry.Write", "UI.ShowNotifications" }
            };
        }
        
        /// <summary>
        /// 初始化插件
        /// </summary>
        /// <param name="context">插件上下文</param>
        /// <returns>初始化结果</returns>
        public async Task<PluginResult> InitializeAsync(IPluginContext context)
        {
            try
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                SetStatus(PluginStatus.Initializing);
                
                _context.Logger.LogInfo($"正在初始化插件: {Metadata.Name}");
                
                // 加载配置
                await LoadConfigAsync();
                
                // 加载壁纸文件列表
                await LoadWallpaperFilesAsync();
                
                // 创建系统托盘图标
                CreateTrayIcon();
                
                SetStatus(PluginStatus.Initialized);
                _context.Logger.LogInfo($"插件初始化成功: {Metadata.Name}");
                
                return PluginResult.Success("插件初始化成功");
            }
            catch (Exception ex)
            {
                SetStatus(PluginStatus.Error);
                _context?.Logger.LogError($"插件初始化失败: {Metadata.Name}", ex);
                return PluginResult.Failure($"插件初始化失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 启动插件
        /// </summary>
        /// <returns>启动结果</returns>
        public async Task<PluginResult> StartAsync()
        {
            try
            {
                if (_status != PluginStatus.Initialized && _status != PluginStatus.Stopped)
                {
                    return PluginResult.Failure($"插件状态不正确，无法启动: {_status}");
                }
                
                SetStatus(PluginStatus.Starting);
                _context?.Logger.LogInfo($"正在启动插件: {Metadata.Name}");
                
                // 检查是否有可用的壁纸文件
                if (_wallpaperFiles.Count == 0)
                {
                    _context?.Logger.LogWarning($"插件 {Metadata.Name} 没有找到可用的壁纸文件");
                    MessageBox.Show("没有找到可用的壁纸文件，请检查配置中的壁纸目录设置。", 
                        "壁纸切换器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SetStatus(PluginStatus.Error);
                    return PluginResult.Failure("没有找到可用的壁纸文件");
                }
                
                // 停止现有定时器
                _timer?.Stop();
                _timer?.Dispose();
                
                // 创建新的定时器
                _timer = new Timer(_config.Interval * 1000); // 转换为毫秒
                _timer.Elapsed += OnTimerElapsed;
                _timer.AutoReset = true;
                _timer.Start();
                
                SetStatus(PluginStatus.Running);
                _context?.Logger.LogInfo($"插件启动成功: {Metadata.Name}, 间隔: {_config.Interval}秒, 壁纸数量: {_wallpaperFiles.Count}");
                
                // 立即切换一次壁纸
                await SwitchWallpaperAsync();
                
                return PluginResult.Success("插件启动成功");
            }
            catch (Exception ex)
            {
                SetStatus(PluginStatus.Error);
                _context?.Logger.LogError($"插件启动失败: {Metadata.Name}", ex);
                return PluginResult.Failure($"插件启动失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 停止插件
        /// </summary>
        /// <returns>停止结果</returns>
        public async Task<PluginResult> StopAsync()
        {
            try
            {
                SetStatus(PluginStatus.Stopping);
                _context?.Logger.LogInfo($"正在停止插件: {Metadata.Name}");
                
                _timer?.Stop();
                _timer?.Dispose();
                _timer = null;
                
                SetStatus(PluginStatus.Stopped);
                _context?.Logger.LogInfo($"插件停止成功: {Metadata.Name}");
                
                await Task.CompletedTask;
                return PluginResult.Success("插件停止成功");
            }
            catch (Exception ex)
            {
                SetStatus(PluginStatus.Error);
                _context?.Logger.LogError($"插件停止失败: {Metadata.Name}", ex);
                return PluginResult.Failure($"插件停止失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 卸载插件
        /// </summary>
        /// <returns>卸载结果</returns>
        public async Task<PluginResult> UnloadAsync()
        {
            try
            {
                SetStatus(PluginStatus.Unloading);
                _context?.Logger.LogInfo($"正在卸载插件: {Metadata.Name}");
                
                // 停止插件
                if (_status == PluginStatus.Running)
                {
                    await StopAsync();
                }
                
                // 清理资源
                _timer?.Dispose();
                
                SetStatus(PluginStatus.Unloaded);
                _context?.Logger.LogInfo($"插件卸载成功: {Metadata.Name}");
                
                return PluginResult.Success("插件卸载成功");
            }
            catch (Exception ex)
            {
                SetStatus(PluginStatus.Error);
                _context?.Logger.LogError($"插件卸载失败: {Metadata.Name}", ex);
                return PluginResult.Failure($"插件卸载失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 显示插件设置界面
        /// </summary>
        public void ShowSettings()
        {
            try
            {
                var settingsForm = new WallpaperSettingsForm(_config);
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    _config = settingsForm.Config;
                    _ = SaveConfigAsync();
                    _ = LoadWallpaperFilesAsync();
                    
                    // 如果插件正在运行，重新启动以应用新配置
                    if (_status == PluginStatus.Running)
                    {
                        _ = Task.Run(async () =>
                        {
                            await StopAsync();
                            await StartAsync();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _context?.Logger.LogError($"显示设置失败: {Metadata.Name}", ex);
                MessageBox.Show($"打开设置失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 获取插件配置模式
        /// </summary>
        /// <returns>配置模式</returns>
        public PluginConfigurationMode GetConfigurationMode()
        {
            return PluginConfigurationMode.Advanced;
        }
        
        /// <summary>
        /// 验证插件依赖
        /// </summary>
        /// <param name="context">插件上下文</param>
        /// <returns>验证结果</returns>
        public async Task<PluginResult> ValidateDependenciesAsync(IPluginContext context)
        {
            await Task.CompletedTask;
            
            // 检查必要的权限
            if (!Environment.OSVersion.Platform.ToString().Contains("Win"))
            {
                return PluginResult.Failure("此插件仅支持Windows操作系统");
            }
            
            return PluginResult.Success("依赖验证通过");
        }
        
        /// <summary>
        /// 设置插件状态
        /// </summary>
        /// <param name="newStatus">新状态</param>
        private void SetStatus(PluginStatus newStatus)
        {
            var oldStatus = _status;
            _status = newStatus;
            
            StatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(
                Metadata.Id, oldStatus, newStatus, "状态由插件管理"));
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        private async Task LoadConfigAsync()
        {
            try
            {
                if (_context == null) return;
                
                var configPath = Path.Combine(_context.ConfigDirectory, "wallpaper_config.json");
                
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    _config = JsonConvert.DeserializeObject<WallpaperConfig>(json) ?? new WallpaperConfig();
                    _context.Logger.LogInfo($"配置加载成功: {configPath}");
                }
                else
                {
                    _context.Logger.LogInfo("配置文件不存在，使用默认配置");
                    _config = new WallpaperConfig();
                    await SaveConfigAsync(); // 保存默认配置
                }
            }
            catch (Exception ex)
            {
                _context?.Logger.LogError($"加载配置失败: {ex.Message}", ex);
                _config = new WallpaperConfig();
            }
        }
        
        /// <summary>
        /// 保存配置
        /// </summary>
        private async Task SaveConfigAsync()
        {
            try
            {
                if (_context == null) return;
                
                var configPath = Path.Combine(_context.ConfigDirectory, "wallpaper_config.json");
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                await File.WriteAllTextAsync(configPath, json);
                _context.Logger.LogInfo($"配置保存成功: {configPath}");
            }
            catch (Exception ex)
            {
                _context?.Logger.LogError($"保存配置失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 加载壁纸文件列表
        /// </summary>
        private async Task LoadWallpaperFilesAsync()
        {
            await Task.Run(() =>
            {
                _wallpaperFiles.Clear();
                
                if (string.IsNullOrEmpty(_config.WallpaperDirectory) || !Directory.Exists(_config.WallpaperDirectory))
                {
                    _context?.Logger.LogWarning($"壁纸目录不存在: {_config.WallpaperDirectory}");
                    return;
                }
                
                try
                {
                    var extensions = _config.FileExtensions.Select(ext => ext.ToLower()).ToList();
                    var files = Directory.GetFiles(_config.WallpaperDirectory, "*.*", SearchOption.AllDirectories)
                        .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()))
                        .ToList();
                    
                    _wallpaperFiles.AddRange(files);
                    _context?.Logger.LogInfo($"加载壁纸文件: {_wallpaperFiles.Count} 个文件");
                }
                catch (Exception ex)
                {
                    _context?.Logger.LogError($"加载壁纸文件失败: {ex.Message}", ex);
                }
            });
        }
        
        /// <summary>
        /// 定时器事件处理
        /// </summary>
        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                await SwitchWallpaperAsync();
            }
            catch (Exception ex)
            {
                _context?.Logger.LogError($"定时器事件处理失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 切换壁纸
        /// </summary>
        private async Task SwitchWallpaperAsync()
        {
            await Task.Run(() =>
            {
                if (_wallpaperFiles.Count == 0)
                    return;
                
                string wallpaperPath;
                
                if (_config.Mode == "random")
                {
                    var random = new Random();
                    wallpaperPath = _wallpaperFiles[random.Next(_wallpaperFiles.Count)];
                }
                else
                {
                    wallpaperPath = _wallpaperFiles[_currentIndex];
                    _currentIndex = (_currentIndex + 1) % _wallpaperFiles.Count;
                }
                
                SetWallpaper(wallpaperPath);
            });
        }
        
        /// <summary>
        /// 创建系统托盘图标
        /// </summary>
        private void CreateTrayIcon()
        {
            // 插件的托盘图标已禁用，由主程序统一管理
            // 如果需要托盘功能，可以通过主程序的插件管理界面访问
            /*
            try
            {
                _trayIcon = new NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    Text = "壁纸切换器 (现代版)",
                    Visible = true
                };
                
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("设置", null, (s, e) => ShowSettings());
                contextMenu.Items.Add("立即切换", null, async (s, e) => await SwitchWallpaperAsync());
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("关于", null, (s, e) => 
                {
                    MessageBox.Show($"{Metadata.Name} v{Metadata.Version}\n{Metadata.Description}\n\n作者: {Metadata.Author}", 
                        "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
                
                _trayIcon.ContextMenuStrip = contextMenu;
                _trayIcon.DoubleClick += (s, e) => ShowSettings();
            }
            catch (Exception ex)
            {
                _context?.Logger.LogError($"创建托盘图标失败: {ex.Message}", ex);
            }
            */
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;
        
        /// <summary>
        /// 设置壁纸
        /// </summary>
        /// <param name="wallpaperPath">壁纸路径</param>
        private void SetWallpaper(string wallpaperPath)
        {
            try
            {
                if (!File.Exists(wallpaperPath))
                {
                    _context?.Logger.LogWarning($"壁纸文件不存在: {wallpaperPath}");
                    return;
                }
                
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wallpaperPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                _context?.Logger.LogInfo($"壁纸切换成功: {Path.GetFileName(wallpaperPath)}");
            }
            catch (Exception ex)
            {
                _context?.Logger.LogError($"设置壁纸失败: {ex.Message}", ex);
            }
        }
    }
}
