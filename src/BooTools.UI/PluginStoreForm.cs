using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using BooTools.Core.Management;
using BooTools.Core.Repository.Management;
using BooTools.Core.Repository.Models;
using BooTools.Core.Repository.Interfaces;
using BooTools.Core.Models;
using BooTools.Core.Interfaces;
using BooTools.Core;

namespace BooTools.UI
{
    /// <summary>
    /// 插件商店窗体
    /// </summary>
    public partial class PluginStoreForm : Form
    {
        private readonly IPluginManager _pluginManager;
        private readonly PluginRepositoryManager _repositoryManager;
        private readonly BooTools.Core.Download.PluginDownloadManager _downloadManager;
        private readonly BooTools.Core.Package.PluginPackageManager _packageManager;
        private readonly ILogger _logger;
        
        // UI 控件
        private TabControl _tabControl = null!;
        private TabPage _browseTab = null!;
        private TabPage _installedTab = null!;
        private TabPage _updatesTab = null!;
        
        // 浏览页面控件
        private TextBox _searchBox = null!;
        private Button _searchButton = null!;
        private ListView _pluginListView = null!;
        private RichTextBox _detailsBox = null!;
        
        // 状态栏
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private ToolStripProgressBar _progressBar = null!;
        
        private CancellationTokenSource _cancellationTokenSource = new();
        
        public PluginStoreForm(IPluginManager pluginManager, IServiceProvider services)
        {
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
            
            // 从服务提供者获取必要的服务
            _repositoryManager = services.GetRequiredService<PluginRepositoryManager>();
            _downloadManager = services.GetRequiredService<BooTools.Core.Download.PluginDownloadManager>();
            _packageManager = services.GetRequiredService<BooTools.Core.Package.PluginPackageManager>();
            _logger = services.GetRequiredService<ILogger>();
            
            InitializeComponent();
            InitializeRepositoryManagerAsync();
        }
        
        private void InitializeComponent()
        {
            this.Text = "插件商店 - Boo Tools";
            this.Size = new Size(1000, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(900, 650);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            
            // 创建选项卡控件
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            
            // 创建浏览选项卡
            CreateBrowseTab();
            
            // 创建已安装选项卡
            CreateInstalledTab();
            
            // 创建更新选项卡
            CreateUpdatesTab();
            
            // 添加选项卡到控件
            _tabControl.TabPages.AddRange(new TabPage[] { _browseTab, _installedTab, _updatesTab });
            
            // 创建状态栏
            CreateStatusBar();
            
            // 添加控件到窗体
            this.Controls.Add(_tabControl);
            this.Controls.Add(_statusStrip);
            
            // 窗体事件
            this.FormClosing += PluginStoreForm_FormClosing;
            this.Load += PluginStoreForm_Load;
        }
        
        private void CreateBrowseTab()
        {
            _browseTab = new TabPage("浏览插件");
            _browseTab.Padding = new Padding(3);

            // 搜索区域
            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5),
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            _searchBox = new TextBox
            {
                Size = new Size(350, 28),
                PlaceholderText = "搜索插件...",
                Margin = new Padding(3)
            };
            _searchBox.KeyDown += SearchBox_KeyDown;

            _searchButton = new Button
            {
                Text = "搜索",
                Size = new Size(80, 30),
                Margin = new Padding(3)
            };
            _searchButton.Click += SearchButton_Click;

            var refreshButton = new Button
            {
                Text = "刷新仓库",
                Size = new Size(100, 30),
                Margin = new Padding(3)
            };
            refreshButton.Click += RefreshButton_Click;

            searchPanel.Controls.AddRange(new Control[] { _searchBox, _searchButton, refreshButton });

            // 分割容器
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400,
                Panel2MinSize = 150
            };

            // 插件列表
            _pluginListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };
            _pluginListView.Columns.Add("插件名称", 200);
            _pluginListView.Columns.Add("版本", 90);
            _pluginListView.Columns.Add("作者", 150);
            _pluginListView.Columns.Add("下载量", 90);
            _pluginListView.Columns.Add("评分", 70);
            _pluginListView.Columns.Add("状态", 90);
            _pluginListView.Columns.Add("仓库", -2);
            _pluginListView.SelectedIndexChanged += PluginListView_SelectedIndexChanged;
            _pluginListView.MouseDoubleClick += PluginListView_MouseDoubleClick;

            // 详情面板
            _detailsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = SystemColors.Window,
                Font = new Font("Microsoft YaHei", 9.5F)
            };

            splitContainer.Panel1.Controls.Add(_pluginListView);
            splitContainer.Panel2.Controls.Add(_detailsBox);

            // 将控件添加到选项卡
            // 添加顺序很重要：先添加填充的控件，再添加停靠在边缘的控件
            _browseTab.Controls.Add(splitContainer);
            _browseTab.Controls.Add(searchPanel);
        }
        
        private ListView _installedPluginListView = null!;

        private void CreateInstalledTab()
        {
            _installedTab = new TabPage("已安装");
            _installedTab.Padding = new Padding(5);

            _installedPluginListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            _installedPluginListView.Columns.Add("插件名称", 250);
            _installedPluginListView.Columns.Add("版本", 120);
            _installedPluginListView.Columns.Add("作者", 200);
            _installedPluginListView.Columns.Add("状态", 120);

            _installedPluginListView.MouseClick += InstalledPluginListView_MouseClick;
            _tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            _installedTab.Controls.Add(_installedPluginListView);
        }

        private async void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_tabControl.SelectedTab == _installedTab)
            {
                await LoadInstalledPluginsAsync();
            }
        }

        private async Task LoadInstalledPluginsAsync()
        {
            try
            {
                SetStatus("正在加载已安装插件列表...", true);
                _installedPluginListView.Items.Clear();

                var installedPlugins = await _pluginManager.GetInstalledPluginsAsync();

                foreach (var plugin in installedPlugins)
                {
                    var item = new ListViewItem(plugin.Metadata.Name);
                    item.SubItems.Add(plugin.Metadata.Version.ToString());
                    item.SubItems.Add(plugin.Metadata.Author);
                    item.SubItems.Add(plugin.Status == PluginStatus.Running ? "已启动" : "已停止");
                    item.Tag = plugin;
                    _installedPluginListView.Items.Add(item);
                }

                SetStatus($"找到 {installedPlugins.Count()} 个已安装的插件", false);
            }
            catch (Exception ex)
            {
                SetStatus($"加载已安装插件失败: {ex.Message}", false);
                MessageBox.Show($"加载已安装插件列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InstalledPluginListView_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _installedPluginListView.SelectedItems.Count > 0)
            {
                var plugin = _installedPluginListView.SelectedItems[0].Tag as IPlugin;
                if (plugin != null)
                {
                    ShowInstalledPluginContextMenu(plugin, e.Location);
                }
            }
        }

        private void ShowInstalledPluginContextMenu(IPlugin plugin, Point location)
        {
            var contextMenu = new ContextMenuStrip();

            if (plugin.Status == PluginStatus.Running)
            {
                contextMenu.Items.Add("停止", null, async (s, e) => await _pluginManager.StopPluginAsync(plugin.Metadata.Id));
            }
            else
            {
                contextMenu.Items.Add("启动", null, async (s, e) => await _pluginManager.StartPluginAsync(plugin.Metadata.Id));
            }

            contextMenu.Items.Add("设置", null, (s, e) => plugin.ShowSettings());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("卸载", null, async (s, e) => await UninstallPluginAsync(plugin));

            contextMenu.Show(_installedPluginListView, location);
        }

        private async Task UninstallPluginAsync(IPlugin plugin)
        {
            var confirmResult = MessageBox.Show(
                $"您确定要卸载插件 '{plugin.Metadata.Name}' 吗？\n\n此操作将从磁盘上永久删除插件文件，且无法恢复。",
                "确认卸载",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    SetStatus($"正在卸载 {plugin.Metadata.Name}...", true);

                    // 1. 停止并卸载插件
                    var unloadResult = await _pluginManager.UnloadPluginAsync(plugin.Metadata.Id);
                    if (!unloadResult.IsSuccess)
                    {
                        // 即便卸载失败，也继续尝试删除文件
                        _logger.LogWarning($"从内存卸载插件 {plugin.Metadata.Id} 失败: {unloadResult.ErrorMessage}");
                    }

                    // 2. 从磁盘删除插件包
                    var pluginsDirectory = Path.Combine(Application.StartupPath, "Plugins");
                    var uninstallResult = await _packageManager.UninstallPackageAsync(plugin.Metadata.Id, pluginsDirectory);

                    if (uninstallResult.IsSuccess)
                    {
                        MessageBox.Show($"插件 '{plugin.Metadata.Name}' 已成功卸载。", "卸载成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"卸载插件 '{plugin.Metadata.Name}' 失败: {uninstallResult.ErrorMessage}", "卸载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"卸载过程中发生未知错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetStatus("卸载操作完成", false);
                    await LoadInstalledPluginsAsync(); // 刷新列表
                }
            }
        }
        
        private void CreateUpdatesTab()
        {
            _updatesTab = new TabPage("更新");
            // TODO: 实现插件更新列表
            var label = new Label
            {
                Text = "插件更新列表 - 功能开发中",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _updatesTab.Controls.Add(label);
        }
        
        private void CreateStatusBar()
        {
            _statusStrip = new StatusStrip();
            
            _statusLabel = new ToolStripStatusLabel
            {
                Text = "就绪",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            _progressBar = new ToolStripProgressBar
            {
                Visible = false,
                Size = new Size(150, 16)
            };
            
            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _progressBar });
        }
        
        private void InitializeRepositoryManagerAsync()
        {
            try
            {
                SetStatus("正在初始化插件仓库...", true);
                
                // 初始化仓库管理器
                // TODO: 添加默认仓库或从配置文件加载
                
                SetStatus("插件仓库初始化完成", false);
            }
            catch (Exception ex)
            {
                SetStatus($"初始化失败: {ex.Message}", false);
                MessageBox.Show($"初始化插件仓库失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private async void PluginStoreForm_Load(object? sender, EventArgs e)
        {
            // 窗体加载时自动搜索所有插件
            await SearchPluginsAsync("");
        }
        
        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SearchButton_Click(sender, e);
            }
        }
        
        private async void SearchButton_Click(object? sender, EventArgs e)
        {
            await SearchPluginsAsync(_searchBox.Text);
        }
        
        private async void RefreshButton_Click(object? sender, EventArgs e)
        {
            SetStatus("正在刷新插件仓库...", true);
            try
            {
                var result = await _repositoryManager.SyncAllRepositoriesAsync(_cancellationTokenSource.Token);
                if (result.IsSuccess)
                {
                    SetStatus("仓库刷新完成", false);
                    await SearchPluginsAsync(_searchBox.Text);
                }
                else
                {
                    SetStatus($"刷新失败: {result.ErrorMessage}", false);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"刷新异常: {ex.Message}", false);
            }
        }
        
        private async Task SearchPluginsAsync(string query)
        {
            
            try
            {
                SetStatus("正在搜索插件...", true);
                _pluginListView.Items.Clear();
                
                var searchResult = await _repositoryManager.SearchAllRepositoriesAsync(
                    query, null, _cancellationTokenSource.Token);
                
                if (searchResult.IsSuccess && searchResult.Data != null)
                {
                    var plugins = searchResult.Data.ToList();
                    
                    foreach (var plugin in plugins)
                    {
                        var item = new ListViewItem(plugin.Metadata.Name);
                        item.SubItems.Add(plugin.Metadata.Version.ToString());
                        item.SubItems.Add(plugin.Metadata.Author);
                        item.SubItems.Add(plugin.Downloads.TotalDownloads.ToString());
                        item.SubItems.Add(plugin.Rating.AverageRating.ToString("F1"));
                        item.SubItems.Add(GetPluginStatusText(plugin));
                        item.SubItems.Add(plugin.RepositoryName);
                        item.Tag = plugin;
                        
                        _pluginListView.Items.Add(item);
                    }
                    
                    SetStatus($"找到 {plugins.Count} 个插件", false);
                }
                else
                {
                    SetStatus($"搜索失败: {searchResult.ErrorMessage}", false);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"搜索异常: {ex.Message}", false);
                MessageBox.Show($"搜索插件失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void PluginListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_pluginListView.SelectedItems.Count > 0)
            {
                var plugin = _pluginListView.SelectedItems[0].Tag as RemotePluginInfo;
                if (plugin != null)
                {
                    ShowPluginDetails(plugin);
                }
            }
            else
            {
                _detailsBox.Clear();
            }
        }
        
        private void PluginListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (_pluginListView.SelectedItems.Count > 0)
            {
                var plugin = _pluginListView.SelectedItems[0].Tag as RemotePluginInfo;
                if (plugin != null)
                {
                    ShowInstallDialog(plugin);
                }
            }
        }
        
        private void ShowPluginDetails(RemotePluginInfo plugin)
        {
            _detailsBox.Clear();
            
            var details = $@"插件名称: {plugin.Metadata.Name}
版本: {plugin.Metadata.Version}
作者: {plugin.Metadata.Author}
分类: {plugin.Metadata.Category}

描述:
{plugin.Metadata.Description}

详细信息:
{plugin.DetailedDescription}

下载统计:
总下载量: {plugin.Downloads.TotalDownloads}
本月下载: {plugin.Downloads.MonthlyDownloads}

评分信息:
平均评分: {plugin.Rating.AverageRating:F1}/5.0 ({plugin.Rating.TotalRatings} 个评分)

系统要求:
最低 .NET 版本: {plugin.Requirements.MinimumDotNetVersion}
所需内存: {plugin.Requirements.RequiredMemoryMB} MB
所需磁盘空间: {plugin.Requirements.RequiredDiskSpaceMB} MB

来源: {plugin.RepositoryName}";

            _detailsBox.Text = details;
        }
        
        private void ShowInstallDialog(RemotePluginInfo plugin)
        {
            var result = MessageBox.Show(
                $"是否安装插件 '{plugin.Metadata.Name}' v{plugin.Metadata.Version}？\n\n" +
                $"作者: {plugin.Metadata.Author}\n" +
                $"描述: {plugin.Metadata.Description}",
                "确认安装",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                InstallPluginAsync(plugin);
            }
        }
        
        private async void InstallPluginAsync(RemotePluginInfo plugin)
        {
            try
            {
                SetStatus($"正在安装插件 {plugin.Metadata.Name}...", true);
                
                // 1. 选择要安装的版本（默认选择最新版本）
                var latestVersion = plugin.Versions.OrderByDescending(v => v.Version).FirstOrDefault();
                if (latestVersion == null)
                {
                    SetStatus("未找到可用版本", false);
                    MessageBox.Show("插件没有可用的版本", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                SetStatus($"正在下载插件包 v{latestVersion.Version}...", true);
                
                // 2. 下载插件包
                var downloadTask = await _downloadManager.AddDownloadTaskAsync(
                    plugin, latestVersion, BooTools.Core.Download.Models.DownloadPriority.Normal, _cancellationTokenSource.Token);
                
                if (!downloadTask.IsSuccess || downloadTask.Data == null)
                {
                    SetStatus($"下载失败: {downloadTask.ErrorMessage}", false);
                    MessageBox.Show($"下载插件失败: {downloadTask.ErrorMessage}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                var task = downloadTask.Data;
                
                // 等待下载完成
                while (task.Status == BooTools.Core.Download.Models.DownloadStatus.Downloading || 
                       task.Status == BooTools.Core.Download.Models.DownloadStatus.Pending)
                {
                    await Task.Delay(500, _cancellationTokenSource.Token);
                    // 这里可以更新进度条
                }
                
                if (task.Status != BooTools.Core.Download.Models.DownloadStatus.Completed)
                {
                    SetStatus("下载失败", false);
                    MessageBox.Show($"插件下载失败: {task.ErrorMessage}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                SetStatus("正在验证插件包...", true);
                
                // 3. 验证插件包
                var verifyResult = await _packageManager.VerifyPackageAsync(
                    task.LocalFilePath, _cancellationTokenSource.Token);
                
                if (!verifyResult.IsSuccess || !verifyResult.Data)
                {
                    SetStatus($"验证失败: {verifyResult.ErrorMessage}", false);
                    MessageBox.Show($"插件包验证失败: {verifyResult.ErrorMessage}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                SetStatus("正在安装插件...", true);
                
                // 4. 安装插件
                var pluginsDirectory = Path.Combine(Application.StartupPath, "Plugins");
                var installDirectory = Path.Combine(pluginsDirectory, plugin.Metadata.Id);
                
                var installResult = await _packageManager.InstallPackageAsync(
                    task.LocalFilePath, installDirectory, _cancellationTokenSource.Token);
                
                if (!installResult.IsSuccess)
                {
                    SetStatus($"安装失败: {installResult.ErrorMessage}", false);
                    MessageBox.Show($"插件安装失败: {installResult.ErrorMessage}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                SetStatus($"插件 {plugin.Metadata.Name} 安装完成", false);
                
                // 5. 通知用户安装成功
                var result = MessageBox.Show(
                    $"插件 '{plugin.Metadata.Name}' v{latestVersion.Version} 安装成功！\n\n" +
                    "是否立即加载此插件？", 
                    "安装完成", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Information);
                
                if (result == DialogResult.Yes)
                {
                    // 6. 尝试加载新安装的插件
                    await LoadNewlyInstalledPluginAsync(installDirectory, plugin.Metadata.Id);
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("安装被取消", false);
            }
            catch (Exception ex)
            {
                SetStatus($"安装失败: {ex.Message}", false);
                MessageBox.Show($"安装插件失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private async Task LoadNewlyInstalledPluginAsync(string installDirectory, string pluginId)
        {
            try
            {
                SetStatus("正在加载新安装的插件...", true);
                
                // 查找插件的主程序集
                var pluginFiles = Directory.GetFiles(installDirectory, "*.dll", SearchOption.TopDirectoryOnly);
                var mainAssembly = pluginFiles.FirstOrDefault(f => 
                    Path.GetFileNameWithoutExtension(f).Equals(pluginId, StringComparison.OrdinalIgnoreCase));
                
                if (mainAssembly == null && pluginFiles.Length > 0)
                {
                    mainAssembly = pluginFiles[0]; // 使用第一个找到的程序集
                }
                
                if (mainAssembly != null)
                {
                    var loadResult = await _pluginManager.LoadPluginAsync(mainAssembly);
                    if (loadResult.IsSuccess)
                    {
                        SetStatus("插件加载成功", false);
                        MessageBox.Show("插件已成功加载，可以在主窗口中查看和管理。", "加载成功", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        SetStatus($"插件加载失败: {loadResult.ErrorMessage}", false);
                        MessageBox.Show($"插件安装成功，但加载失败: {loadResult.ErrorMessage}\n\n" +
                                      "请重启程序后再试。", "加载失败", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    SetStatus("未找到插件程序集", false);
                    MessageBox.Show("插件安装成功，但未找到主程序集。请重启程序后再试。", "加载失败", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"加载插件失败: {ex.Message}", false);
                MessageBox.Show($"插件安装成功，但加载失败: {ex.Message}\n\n请重启程序后再试。", "加载失败", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private string GetPluginStatusText(RemotePluginInfo plugin)
        {
            // TODO: 检查插件是否已安装
            return plugin.Status switch
            {
                ReleaseStatus.Stable => "稳定版",
                ReleaseStatus.Beta => "测试版",
                ReleaseStatus.Alpha => "内测版",
                ReleaseStatus.Development => "开发版",
                _ => "未知"
            };
        }
        
        private void SetStatus(string message, bool showProgress)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, bool>(SetStatus), message, showProgress);
                return;
            }
            
            _statusLabel.Text = message;
            _progressBar.Visible = showProgress;
            
            if (showProgress)
            {
                _progressBar.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                _progressBar.Style = ProgressBarStyle.Continuous;
            }
        }
        
        private void PluginStoreForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                // 注意：这些服务是单例，由依赖注入容器管理，不应该在这里释放
                // _repositoryManager, _downloadManager, _packageManager 由服务容器管理
            }
            base.Dispose(disposing);
        }
    }
}
