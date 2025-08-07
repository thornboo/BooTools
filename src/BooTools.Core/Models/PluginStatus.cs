namespace BooTools.Core.Models
{
    /// <summary>
    /// 插件状态枚举
    /// </summary>
    public enum PluginStatus
    {
        /// <summary>
        /// 未知状态
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// 已安装但未初始化
        /// </summary>
        Installed = 1,
        
        /// <summary>
        /// 正在初始化
        /// </summary>
        Initializing = 2,
        
        /// <summary>
        /// 已初始化但未启动
        /// </summary>
        Initialized = 3,
        
        /// <summary>
        /// 正在启动
        /// </summary>
        Starting = 4,
        
        /// <summary>
        /// 运行中
        /// </summary>
        Running = 5,
        
        /// <summary>
        /// 正在停止
        /// </summary>
        Stopping = 6,
        
        /// <summary>
        /// 已停止
        /// </summary>
        Stopped = 7,
        
        /// <summary>
        /// 正在卸载
        /// </summary>
        Unloading = 8,
        
        /// <summary>
        /// 已卸载
        /// </summary>
        Unloaded = 9,
        
        /// <summary>
        /// 错误状态
        /// </summary>
        Error = 10,
        
        /// <summary>
        /// 已禁用
        /// </summary>
        Disabled = 11
    }
}
