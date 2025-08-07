using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BooTools.Core.Configuration
{
    /// <summary>
    /// 插件配置信息
    /// </summary>
    public class PluginConfiguration
    {
        /// <summary>
        /// 插件ID
        /// </summary>
        public string PluginId { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        
        /// <summary>
        /// 是否自动启动
        /// </summary>
        public bool AutoStart { get; set; } = true;
        
        /// <summary>
        /// 配置数据
        /// </summary>
        public Dictionary<string, JsonElement> Settings { get; set; } = new();
        
        /// <summary>
        /// 配置版本
        /// </summary>
        public Version ConfigVersion { get; set; } = new Version(1, 0, 0);
        
        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public T GetValue<T>(string key, T defaultValue = default!)
        {
            if (!Settings.TryGetValue(key, out var element))
            {
                return defaultValue;
            }
            
            try
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText()) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// 设置配置值
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        public void SetValue<T>(string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            Settings[key] = JsonSerializer.Deserialize<JsonElement>(json);
            LastUpdated = DateTime.Now;
        }
        
        /// <summary>
        /// 移除配置值
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveValue(string key)
        {
            var result = Settings.Remove(key);
            if (result)
            {
                LastUpdated = DateTime.Now;
            }
            return result;
        }
        
        /// <summary>
        /// 检查是否存在配置键
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>是否存在</returns>
        public bool ContainsKey(string key)
        {
            return Settings.ContainsKey(key);
        }
        
        /// <summary>
        /// 获取所有配置键
        /// </summary>
        /// <returns>配置键集合</returns>
        public IEnumerable<string> GetKeys()
        {
            return Settings.Keys;
        }
        
        /// <summary>
        /// 清空所有配置
        /// </summary>
        public void Clear()
        {
            Settings.Clear();
            LastUpdated = DateTime.Now;
        }
    }
}
