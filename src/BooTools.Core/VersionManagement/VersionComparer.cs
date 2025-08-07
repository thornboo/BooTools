using System;
using System.Collections.Generic;
using System.Linq;
using BooTools.Core.VersionManagement.Models;

namespace BooTools.Core.VersionManagement
{
    /// <summary>
    /// 版本比较器
    /// </summary>
    public static class VersionComparer
    {
        /// <summary>
        /// 比较两个版本号
        /// </summary>
        /// <param name="version1">版本1</param>
        /// <param name="version2">版本2</param>
        /// <returns>比较结果：-1表示version1小于version2，0表示相等，1表示version1大于version2</returns>
        public static int Compare(Version version1, Version version2)
        {
            if (version1 == null && version2 == null) return 0;
            if (version1 == null) return -1;
            if (version2 == null) return 1;

            return version1.CompareTo(version2);
        }

        /// <summary>
        /// 比较两个版本信息
        /// </summary>
        /// <param name="info1">版本信息1</param>
        /// <param name="info2">版本信息2</param>
        /// <returns>比较结果</returns>
        public static int Compare(VersionInfo? info1, VersionInfo? info2)
        {
            if (info1 == null && info2 == null) return 0;
            if (info1 == null) return -1;
            if (info2 == null) return 1;

            // 首先比较主版本号
            var versionComparison = Compare(info1.Version, info2.Version);
            if (versionComparison != 0) return versionComparison;

            // 如果主版本号相同，比较预发布标签
            return ComparePrereleaseTags(info1.PrereleaseTag, info2.PrereleaseTag);
        }

        /// <summary>
        /// 比较预发布标签
        /// </summary>
        /// <param name="tag1">标签1</param>
        /// <param name="tag2">标签2</param>
        /// <returns>比较结果</returns>
        private static int ComparePrereleaseTags(string? tag1, string? tag2)
        {
            // 如果两个都是正式版本（无预发布标签），则相等
            if (string.IsNullOrEmpty(tag1) && string.IsNullOrEmpty(tag2)) return 0;
            
            // 正式版本大于预发布版本
            if (string.IsNullOrEmpty(tag1)) return 1;
            if (string.IsNullOrEmpty(tag2)) return -1;

            // 比较预发布标签
            var parts1 = tag1.Split('.');
            var parts2 = tag2.Split('.');

            var maxLength = Math.Max(parts1.Length, parts2.Length);
            for (int i = 0; i < maxLength; i++)
            {
                var part1 = i < parts1.Length ? parts1[i] : "";
                var part2 = i < parts2.Length ? parts2[i] : "";

                var comparison = ComparePrereleasePart(part1, part2);
                if (comparison != 0) return comparison;
            }

            return 0;
        }

        /// <summary>
        /// 比较预发布标签的组成部分
        /// </summary>
        /// <param name="part1">部分1</param>
        /// <param name="part2">部分2</param>
        /// <returns>比较结果</returns>
        private static int ComparePrereleasePart(string part1, string part2)
        {
            // 如果两个部分都为空，则相等
            if (string.IsNullOrEmpty(part1) && string.IsNullOrEmpty(part2)) return 0;
            
            // 空部分小于非空部分
            if (string.IsNullOrEmpty(part1)) return -1;
            if (string.IsNullOrEmpty(part2)) return 1;

            // 尝试解析为数字进行比较
            if (int.TryParse(part1, out var num1) && int.TryParse(part2, out var num2))
            {
                return num1.CompareTo(num2);
            }

            // 如果无法解析为数字，按字符串比较
            return string.Compare(part1, part2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查版本兼容性
        /// </summary>
        /// <param name="pluginVersion">插件版本</param>
        /// <param name="hostVersion">主机版本</param>
        /// <param name="compatibleVersions">兼容版本范围</param>
        /// <returns>是否兼容</returns>
        public static bool IsCompatible(Version pluginVersion, Version hostVersion, string compatibleVersions)
        {
            if (string.IsNullOrWhiteSpace(compatibleVersions) || compatibleVersions == "*")
                return true;

            // 解析兼容版本范围
            var ranges = ParseVersionRanges(compatibleVersions);
            
            foreach (var range in ranges)
            {
                if (IsVersionInRange(hostVersion, range))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 解析版本范围字符串
        /// </summary>
        /// <param name="versionRanges">版本范围字符串</param>
        /// <returns>版本范围列表</returns>
        private static List<VersionRange> ParseVersionRanges(string versionRanges)
        {
            var ranges = new List<VersionRange>();
            var parts = versionRanges.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (VersionRange.TryParse(trimmed, out var range))
                {
                    ranges.Add(range);
                }
            }

            return ranges;
        }

        /// <summary>
        /// 检查版本是否在指定范围内
        /// </summary>
        /// <param name="version">版本</param>
        /// <param name="range">版本范围</param>
        /// <returns>是否在范围内</returns>
        private static bool IsVersionInRange(Version version, VersionRange range)
        {
            if (range.MinVersion != null && version < range.MinVersion)
                return false;

            if (range.MaxVersion != null && version > range.MaxVersion)
                return false;

            return true;
        }

        /// <summary>
        /// 获取最新版本
        /// </summary>
        /// <param name="versions">版本列表</param>
        /// <returns>最新版本</returns>
        public static VersionInfo? GetLatestVersion(IEnumerable<VersionInfo> versions)
        {
            return versions.OrderByDescending(v => v, new VersionInfoComparer()).FirstOrDefault();
        }

        /// <summary>
        /// 获取稳定版本
        /// </summary>
        /// <param name="versions">版本列表</param>
        /// <returns>最新稳定版本</returns>
        public static VersionInfo? GetLatestStableVersion(IEnumerable<VersionInfo> versions)
        {
            return versions
                .Where(v => string.IsNullOrEmpty(v.PrereleaseTag))
                .OrderByDescending(v => v, new VersionInfoComparer())
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// 版本范围
    /// </summary>
    public class VersionRange
    {
        public Version? MinVersion { get; set; }
        public Version? MaxVersion { get; set; }
        public bool IncludeMin { get; set; } = true;
        public bool IncludeMax { get; set; } = true;

        public static bool TryParse(string rangeString, out VersionRange range)
        {
            range = new VersionRange();

            if (string.IsNullOrWhiteSpace(rangeString))
                return false;

            // 支持格式：1.0.0, [1.0.0,2.0.0], (1.0.0,2.0.0], [1.0.0,2.0.0)
            rangeString = rangeString.Trim();

            if (rangeString.StartsWith("[") && rangeString.EndsWith("]"))
            {
                // 闭区间 [min,max]
                range.IncludeMin = true;
                range.IncludeMax = true;
                rangeString = rangeString.Substring(1, rangeString.Length - 2);
            }
            else if (rangeString.StartsWith("(") && rangeString.EndsWith(")"))
            {
                // 开区间 (min,max)
                range.IncludeMin = false;
                range.IncludeMax = false;
                rangeString = rangeString.Substring(1, rangeString.Length - 2);
            }
            else if (rangeString.StartsWith("[") && rangeString.EndsWith(")"))
            {
                // 左闭右开 [min,max)
                range.IncludeMin = true;
                range.IncludeMax = false;
                rangeString = rangeString.Substring(1, rangeString.Length - 2);
            }
            else if (rangeString.StartsWith("(") && rangeString.EndsWith("]"))
            {
                // 左开右闭 (min,max]
                range.IncludeMin = false;
                range.IncludeMax = true;
                rangeString = rangeString.Substring(1, rangeString.Length - 2);
            }
            else
            {
                // 单个版本号
                if (Version.TryParse(rangeString, out var version))
                {
                    range.MinVersion = version;
                    range.MaxVersion = version;
                    return true;
                }
                return false;
            }

            // 解析范围
            var parts = rangeString.Split(',');
            if (parts.Length == 2)
            {
                var minStr = parts[0].Trim();
                var maxStr = parts[1].Trim();

                if (!string.IsNullOrEmpty(minStr) && Version.TryParse(minStr, out var minVersion))
                {
                    range.MinVersion = minVersion;
                }

                if (!string.IsNullOrEmpty(maxStr) && Version.TryParse(maxStr, out var maxVersion))
                {
                    range.MaxVersion = maxVersion;
                }

                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 版本信息比较器
    /// </summary>
    public class VersionInfoComparer : IComparer<VersionInfo>
    {
        public int Compare(VersionInfo? x, VersionInfo? y)
        {
            return VersionComparer.Compare(x, y);
        }
    }
}
