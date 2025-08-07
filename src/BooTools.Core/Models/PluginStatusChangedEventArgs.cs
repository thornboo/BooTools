using System;

namespace BooTools.Core.Models
{
    /// <summary>
    /// 插件状态变化事件参数
    /// </summary>
    public class PluginStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 插件ID
        /// </summary>
        public string PluginId { get; }
        
        /// <summary>
        /// 旧状态
        /// </summary>
        public PluginStatus OldStatus { get; }
        
        /// <summary>
        /// 新状态
        /// </summary>
        public PluginStatus NewStatus { get; }
        
        /// <summary>
        /// 状态变化时间
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// 状态变化原因
        /// </summary>
        public string? Reason { get; }
        
        /// <summary>
        /// 初始化插件状态变化事件参数
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="oldStatus">旧状态</param>
        /// <param name="newStatus">新状态</param>
        /// <param name="reason">变化原因</param>
        public PluginStatusChangedEventArgs(string pluginId, PluginStatus oldStatus, PluginStatus newStatus, string? reason = null)
        {
            PluginId = pluginId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Reason = reason;
            Timestamp = DateTime.Now;
        }
    }
}
