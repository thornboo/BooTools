using System;

namespace BooTools.Core
{
    /// <summary>
    /// Boo Tools 插件接口
    /// </summary>
    public interface IBooPlugin
    {
        /// <summary>
        /// 插件名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 插件描述
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 插件版本
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// 插件作者
        /// </summary>
        string Author { get; }
        
        /// <summary>
        /// 插件图标路径
        /// </summary>
        string IconPath { get; }
        
        /// <summary>
        /// 初始化插件
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 启动插件
        /// </summary>
        void Start();
        
        /// <summary>
        /// 停止插件
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 显示插件设置界面
        /// </summary>
        void ShowSettings();
        
        /// <summary>
        /// 插件状态
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// 插件状态改变事件
        /// </summary>
        event EventHandler<bool> StatusChanged;
    }
} 