using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using BooTools.Core;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace BooTools.Plugins.WallpaperSwitcher
{
    public class WallpaperSwitcherPlugin : IBooPlugin
    {
        private System.Timers.Timer? _timer;
        private WallpaperConfig _config = new();
        private List<string> _wallpaperFiles;
        private int _currentIndex;
        private bool _isInitialized = false;
        
        public string Name => "壁纸切换器";
        public string Description => "定时自动切换桌面壁纸";
        public string Version => "1.0.0";
        public string Author => "thornboo";
        public string IconPath => "icon.png";
        
        public bool IsEnabled { get; set; }
        
        public event EventHandler<bool>? StatusChanged;
        
        public WallpaperSwitcherPlugin()
        {
            _wallpaperFiles = new List<string>();
            _currentIndex = 0;
            IsEnabled = false; // 默认未启动
            LoadConfig();
        }
        
        public void Initialize()
        {
            try
            {
                Console.WriteLine($"正在初始化插件: {Name}");
                
                // 加载壁纸文件列表
                LoadWallpaperFiles();
                
                // 创建系统托盘图标
                CreateTrayIcon();
                
                _isInitialized = true;
                Console.WriteLine($"插件初始化成功: {Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"插件初始化失败: {Name}, 错误: {ex.Message}");
                throw;
            }
        }
        
        public void Start()
        {
            try
            {
                if (!_isInitialized)
                {
                    Initialize();
                }
                
                // 检查是否有可用的壁纸文件
                if (_wallpaperFiles.Count == 0)
                {
                    Console.WriteLine($"警告: 插件 {Name} 没有找到可用的壁纸文件");
                    MessageBox.Show("没有找到可用的壁纸文件，请检查配置中的壁纸目录设置。", 
                        "壁纸切换器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // 停止现有定时器
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Dispose();
                }
                
                // 创建新的定时器
                _timer = new Timer(_config.Interval * 1000); // 转换为毫秒
                _timer.Elapsed += OnTimerElapsed;
                _timer.AutoReset = true;
                _timer.Start();
                
                IsEnabled = true;
                StatusChanged?.Invoke(this, true);
                
                Console.WriteLine($"插件启动成功: {Name}, 间隔: {_config.Interval}秒, 壁纸数量: {_wallpaperFiles.Count}");
                
                // 立即切换一次壁纸
                SwitchWallpaper();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"插件启动失败: {Name}, 错误: {ex.Message}");
                IsEnabled = false;
                StatusChanged?.Invoke(this, false);
                throw;
            }
        }
        
        public void Stop()
        {
            try
            {
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Dispose();
                    _timer = null!;
                }
                
                IsEnabled = false;
                StatusChanged?.Invoke(this, false);
                
                Console.WriteLine($"插件停止成功: {Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"插件停止失败: {Name}, 错误: {ex.Message}");
            }
        }
        
        public void ShowSettings()
        {
            try
            {
                var settingsForm = new WallpaperSettingsForm(_config);
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    _config = settingsForm.Config;
                    SaveConfig();
                    LoadWallpaperFiles();
                    
                    // 如果插件正在运行，重新启动以应用新配置
                    if (IsEnabled)
                    {
                        Stop();
                        Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"显示设置失败: {Name}, 错误: {ex.Message}");
                MessageBox.Show($"打开设置失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadConfig()
        {
            try
            {
                var configPath = Path.Combine(Application.StartupPath, "config", "wallpaper_config.json");
                var configDir = Path.GetDirectoryName(configPath);
                
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);
                
                if (File.Exists(configPath))
                {
                    try
                    {
                        var json = File.ReadAllText(configPath);
                        _config = JsonConvert.DeserializeObject<WallpaperConfig>(json) ?? new WallpaperConfig();
                        Console.WriteLine($"配置加载成功: {configPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"配置文件解析失败: {ex.Message}, 使用默认配置");
                        _config = new WallpaperConfig();
                    }
                }
                else
                {
                    Console.WriteLine("配置文件不存在，使用默认配置");
                    _config = new WallpaperConfig();
                    SaveConfig(); // 保存默认配置
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置失败: {ex.Message}");
                _config = new WallpaperConfig();
            }
        }
        
        private void SaveConfig()
        {
            try
            {
                var configPath = Path.Combine(Application.StartupPath, "config", "wallpaper_config.json");
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(configPath, json);
                Console.WriteLine($"配置保存成功: {configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置失败: {ex.Message}");
            }
        }
        
        private void LoadWallpaperFiles()
        {
            _wallpaperFiles.Clear();
            
            if (string.IsNullOrEmpty(_config.WallpaperDirectory) || !Directory.Exists(_config.WallpaperDirectory))
            {
                Console.WriteLine($"壁纸目录不存在: {_config.WallpaperDirectory}");
                return;
            }
            
            try
            {
                var extensions = _config.FileExtensions.Select(ext => ext.ToLower()).ToList();
                var files = Directory.GetFiles(_config.WallpaperDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()))
                    .ToList();
                
                _wallpaperFiles.AddRange(files);
                Console.WriteLine($"加载壁纸文件: {_wallpaperFiles.Count} 个文件");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载壁纸文件失败: {ex.Message}");
            }
        }
        
        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                SwitchWallpaper();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"定时器事件处理失败: {ex.Message}");
            }
        }
        
        private void SwitchWallpaper()
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
        }
        
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
                    Text = "壁纸切换器",
                    Visible = true
                };
                
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("设置", null, (s, e) => ShowSettings());
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("退出", null, (s, e) => Application.Exit());
                
                _trayIcon.ContextMenuStrip = contextMenu;
                _trayIcon.DoubleClick += (s, e) => ShowSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建托盘图标失败: {ex.Message}");
            }
            */
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;
        
        private void SetWallpaper(string wallpaperPath)
        {
            try
            {
                if (!File.Exists(wallpaperPath))
                {
                    Console.WriteLine($"壁纸文件不存在: {wallpaperPath}");
                    return;
                }
                
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wallpaperPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                Console.WriteLine($"壁纸切换成功: {Path.GetFileName(wallpaperPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置壁纸失败: {ex.Message}");
            }
        }
    }
    
    public class WallpaperConfig
    {
        public bool Enabled { get; set; } = true;
        public int Interval { get; set; } = 300; // 秒
        public string Mode { get; set; } = "random"; // random, sequential
        public string WallpaperDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public List<string> FileExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png", ".bmp" };
    }
} 