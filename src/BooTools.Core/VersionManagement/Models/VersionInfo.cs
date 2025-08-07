using System;
using System.Collections.Generic;
using BooTools.Core.Models;

namespace BooTools.Core.VersionManagement.Models
{
    /// <summary>
    /// 版本信息模型
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// 版本号
        /// </summary>
        public Version Version { get; set; } = new Version(1, 0, 0);

        /// <summary>
        /// 版本标签（如 alpha, beta, rc 等）
        /// </summary>
        public string? PrereleaseTag { get; set; }

        /// <summary>
        /// 构建号
        /// </summary>
        public string? BuildNumber { get; set; }

        /// <summary>
        /// 发布日期
        /// </summary>
        public DateTime ReleaseDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新说明
        /// </summary>
        public string? ReleaseNotes { get; set; }

        /// <summary>
        /// 下载地址
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 校验和
        /// </summary>
        public string? Checksum { get; set; }

        /// <summary>
        /// 是否强制更新
        /// </summary>
        public bool IsForcedUpdate { get; set; }

        /// <summary>
        /// 兼容的主机版本范围
        /// </summary>
        public string CompatibleHostVersions { get; set; } = "*";

        /// <summary>
        /// 最低 .NET 版本要求
        /// </summary>
        public string MinimumDotNetVersion { get; set; } = "8.0";

        /// <summary>
        /// 依赖项
        /// </summary>
        public List<PluginDependency> Dependencies { get; set; } = new();

        /// <summary>
        /// 从字符串解析版本信息
        /// </summary>
        /// <param name="versionString">版本字符串</param>
        /// <returns>版本信息</returns>
        public static VersionInfo Parse(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return new VersionInfo();

            var info = new VersionInfo();
            
            // 解析版本号部分
            var parts = versionString.Split('-');
            if (Version.TryParse(parts[0], out var version))
            {
                info.Version = version;
            }

            // 解析预发布标签
            if (parts.Length > 1)
            {
                var prereleaseParts = parts[1].Split('+');
                info.PrereleaseTag = prereleaseParts[0];
                
                // 解析构建号
                if (prereleaseParts.Length > 1)
                {
                    info.BuildNumber = prereleaseParts[1];
                }
            }

            return info;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns>版本字符串</returns>
        public override string ToString()
        {
            var result = Version.ToString();
            
            if (!string.IsNullOrEmpty(PrereleaseTag))
            {
                result += $"-{PrereleaseTag}";
            }
            
            if (!string.IsNullOrEmpty(BuildNumber))
            {
                result += $"+{BuildNumber}";
            }
            
            return result;
        }

        /// <summary>
        /// 获取完整版本信息
        /// </summary>
        /// <returns>完整版本信息字符串</returns>
        public string GetFullVersionString()
        {
            var result = ToString();
            
            if (ReleaseDate != DateTime.MinValue)
            {
                result += $" ({ReleaseDate:yyyy-MM-dd})";
            }
            
            return result;
        }
    }
}
