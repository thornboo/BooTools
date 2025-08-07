using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BooTools.Core;
using BooTools.Core.Management;
using BooTools.Core.Models;
using BooTools.Core.Interfaces;

namespace BooTools.UI
{
    public partial class MainForm : Form
    {
        private readonly IPluginManager _pluginManager;
        private readonly IServiceProvider _services;
        private MenuStrip _menuStrip = null!;
        private ListView _pluginListView = null!;
        private Button _btnRefresh = null!;
        private NotifyIcon _trayIcon = null!;
        private ToolStripMenuItem _minimizeOnCloseMenuItem = null!;
        private bool _minimizeOnClose = true; // 默认为 true
        
        public MainForm(IPluginManager pluginManager, IServiceProvider services)
        {
            _pluginManager = pluginManager;
            _services = services;
            InitializeComponent();
            
            // 修正：必须先创建托盘图标对象，再为其设置图标
            CreateTrayIcon();
            SetAppIcon();
            
            LoadPluginsAsync();
            
            // 订阅插件状态变化事件
            _pluginManager.PluginStatusChanged += OnPluginStatusChanged;
        }

        private void SetAppIcon()
        {
            try
            {
                // 从相对路径加载图标
                var iconPath = System.IO.Path.Combine(Application.StartupPath, "assets", "logo.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    var appIcon = new Icon(iconPath);
                    this.Icon = appIcon;
                    if (_trayIcon != null)
                    {
                        _trayIcon.Icon = appIcon;
                    }
                }
            }
            catch (Exception ex)
            {
                // 图标加载失败时，不影响程序运行
                Console.WriteLine($"加载图标失败: {ex.Message}");
            }
        }
        
        private void InitializeComponent()
        {
            // 启用DPI感知和自动缩放
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            
            this.Text = "Boo Tools - Windows 工具箱";
            this.Size = new Size(1200, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(975, 750); // 设置最小尺寸
            this.Padding = new Padding(10); // 为整个窗口提供边距

            // --- 菜单栏 ---
            _menuStrip = new MenuStrip();
            this.MainMenuStrip = _menuStrip;
            
            // 文件菜单
            var fileMenuItem = new ToolStripMenuItem("文件(&F)");
            var refreshMenuItem = new ToolStripMenuItem("刷新插件(&R)");
            refreshMenuItem.Click += BtnRefresh_Click; // 复用现有逻辑
            var exitMenuItem = new ToolStripMenuItem("退出(&X)");
            exitMenuItem.Click += (s, e) => 
            {
                _minimizeOnClose = false; // 确保能真正退出
                Application.Exit();
            };
            fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] { refreshMenuItem, new ToolStripSeparator(), exitMenuItem });

            // 设置菜单
            var settingsMenuItem = new ToolStripMenuItem("设置(&S)");
            _minimizeOnCloseMenuItem = new ToolStripMenuItem("关闭时最小化到托盘");
            _minimizeOnCloseMenuItem.CheckOnClick = true;
            _minimizeOnCloseMenuItem.Checked = _minimizeOnClose; // 从字段初始化
            _minimizeOnCloseMenuItem.CheckedChanged += MinimizeOnCloseMenuItem_CheckedChanged;
            settingsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _minimizeOnCloseMenuItem });

            var storeMenuItem = new ToolStripMenuItem("插件商店(&P)");
            storeMenuItem.Click += StoreMenuItem_Click;
            
            var debugMenuItem = new ToolStripMenuItem("调试(&D)");
            var consoleMenuItem = new ToolStripMenuItem("控制台(&C)");
            consoleMenuItem.Click += ConsoleMenuItem_Click;
            var debugConfigMenuItem = new ToolStripMenuItem("调试配置(&D)");
            debugConfigMenuItem.Click += DebugConfigMenuItem_Click;
            debugMenuItem.DropDownItems.AddRange(new ToolStripItem[] { consoleMenuItem, debugConfigMenuItem });
            
            var aboutMenuItem = new ToolStripMenuItem("关于(&A)");
            aboutMenuItem.Click += AboutMenuItem_Click;
            
            _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenuItem, settingsMenuItem, storeMenuItem, debugMenuItem, aboutMenuItem });

            // --- 插件列表 ---
            _pluginListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill // 关键：填满剩余空间
            };
            
            _pluginListView.Columns.Add("插件名称", 200);
            _pluginListView.Columns.Add("描述", 350);
            _pluginListView.Columns.Add("版本", 100);
            _pluginListView.Columns.Add("状态", 120);
            _pluginListView.Columns.Add("作者", 150);
            
            _pluginListView.DoubleClick += PluginListView_DoubleClick;
            _pluginListView.MouseClick += PluginListView_MouseClick;
            
            // --- 添加控件到主窗口 ---
            // 停靠顺序很重要：先添加四周的，再添加填充的
            this.Controls.Add(_pluginListView);
            this.Controls.Add(_menuStrip);
            
            // 窗体关闭事件
            this.FormClosing += MainForm_FormClosing;
        }
        
        private async void LoadPluginsAsync()
        {
            try
            {
                _pluginListView.Items.Clear();
                
                var plugins = await _pluginManager.GetInstalledPluginsAsync();
                
                foreach (var plugin in plugins)
                {
                    var item = new ListViewItem(plugin.Metadata.Name);
                    item.SubItems.Add(plugin.Metadata.Description);
                    item.SubItems.Add(plugin.Metadata.Version.ToString());
                    item.SubItems.Add(GetStatusDisplayText(plugin.Status));
                    item.SubItems.Add(plugin.Metadata.Author);
                    item.Tag = plugin;
                    
                    // 根据状态设置颜色（只对状态列设置颜色）
                    item.SubItems[3].BackColor = GetStatusColor(plugin.Status);
                    
                    _pluginListView.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载插件列表失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private string GetStatusDisplayText(PluginStatus status)
        {
            return status switch
            {
                PluginStatus.Running => "已启动",
                _ => "已停止"
            };
        }
        
        private Color GetStatusColor(PluginStatus status)
        {
            return status switch
            {
                PluginStatus.Running => Color.LightGreen,
                _ => Color.White
            };
        }
        
        private void PluginListView_DoubleClick(object? sender, EventArgs e)
        {
            if (_pluginListView.SelectedItems.Count > 0)
            {
                var plugin = _pluginListView.SelectedItems[0].Tag as IPlugin;
                if (plugin != null)
                {
                    plugin.ShowSettings();
                    LoadPluginsAsync(); // 刷新列表
                }
            }
        }
        
        private void PluginListView_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _pluginListView.SelectedItems.Count > 0)
            {
                var plugin = _pluginListView.SelectedItems[0].Tag as IPlugin;
                if (plugin != null)
                {
                    ShowPluginContextMenu(plugin, e.Location);
                }
            }
        }
        
        private void ShowPluginContextMenu(IPlugin plugin, Point location)
        {
            var contextMenu = new ContextMenuStrip();
            
            // 启动/停止菜单项
            if (plugin.Status == PluginStatus.Running)
            {
                contextMenu.Items.Add("停止插件", null, async (s, e) => 
                {
                    await _pluginManager.StopPluginAsync(plugin.Metadata.Id);
                    LoadPluginsAsync();
                });
            }
            else if (plugin.Status == PluginStatus.Stopped || plugin.Status == PluginStatus.Initialized)
            {
                contextMenu.Items.Add("启动插件", null, async (s, e) => 
                {
                    await _pluginManager.StartPluginAsync(plugin.Metadata.Id);
                    LoadPluginsAsync();
                });
            }
            
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("设置", null, (s, e) => 
            {
                plugin.ShowSettings();
                LoadPluginsAsync();
            });
            
            // 添加启用/禁用选项
            contextMenu.Items.Add("-");
            if (plugin.Status != PluginStatus.Disabled)
            {
                contextMenu.Items.Add("禁用插件", null, async (s, e) => 
                {
                    await _pluginManager.SetPluginEnabledAsync(plugin.Metadata.Id, false);
                    LoadPluginsAsync();
                });
            }
            else
            {
                contextMenu.Items.Add("启用插件", null, async (s, e) => 
                {
                    await _pluginManager.SetPluginEnabledAsync(plugin.Metadata.Id, true);
                    LoadPluginsAsync();
                });
            }
            
            contextMenu.Show(_pluginListView, location);
        }
        
        private void OnPluginStatusChanged(object? sender, PluginStatusChangedEventArgs e)
        {
            // 在UI线程中更新界面
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnPluginStatusChanged(sender, e)));
                return;
            }
            
            LoadPluginsAsync(); // 刷新插件列表
        }
        
        private async void BtnRefresh_Click(object? sender, EventArgs e)
        {
            try
            {
                var result = await _pluginManager.RefreshPluginsAsync();
                if (!result.IsSuccess)
                {
                    MessageBox.Show($"刷新插件时发生错误:\n{result.ErrorMessage}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                LoadPluginsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新插件失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void CreateTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Boo Tools",
                Visible = true
            };
            
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("显示主界面", null, (s, e) => 
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            });
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (s, e) => 
            {
                _minimizeOnClose = false; // 确保能真正退出
                Application.Exit();
            });
            
            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.DoubleClick += (s, e) => 
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            };
        }
        
        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // 仅当用户通过UI关闭窗口且设置了最小化到托盘时，才取消关闭
            if (_minimizeOnClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void MinimizeOnCloseMenuItem_CheckedChanged(object? sender, EventArgs e)
        {
            _minimizeOnClose = _minimizeOnCloseMenuItem.Checked;
            // TODO: 将此设置持久化到配置文件
        }
        
        // 插件商店菜单事件处理
        private void StoreMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                var storeForm = new PluginStoreForm(_pluginManager, _services);
                storeForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开插件商店失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // 控制台菜单事件处理（原日志查看器）
        private void ConsoleMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                // 从依赖注入容器获取日志服务
                var logger = new ConsoleLogger(true); // 临时解决方案
                var logViewerForm = new LogViewerForm(logger);
                logViewerForm.Text = "控制台 - 日志查看器";
                logViewerForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开控制台失败: {ex.Message}", "控制台", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // 调试配置菜单事件处理
        private void DebugConfigMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                // 从依赖注入容器获取日志服务
                var logger = new ConsoleLogger(true); // 临时解决方案
                var debugConfigForm = new DebugConfigForm(logger);
                debugConfigForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开调试配置失败: {ex.Message}", "调试配置", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // 关于菜单事件处理
        private void AboutMenuItem_Click(object? sender, EventArgs e)
        {
            var aboutInfo = "Boo Tools - Windows 工具箱\n\n";
            aboutInfo += "版本: 1.0.0\n";
            aboutInfo += "作者: Boo Tools Team\n";
            aboutInfo += "描述: 一个功能丰富的Windows工具箱，支持插件扩展";
            
            MessageBox.Show(aboutInfo, "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
} 