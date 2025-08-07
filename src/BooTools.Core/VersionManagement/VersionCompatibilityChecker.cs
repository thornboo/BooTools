using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BooTools.Core.VersionManagement.Models;
using BooTools.Core.Models;

namespace BooTools.Core.VersionManagement
{
    /// <summary>
    /// 版本兼容性检查器
    /// </summary>
    public class VersionCompatibilityChecker
    {
        /// <summary>
        /// 检查插件与主程序的兼容性
        /// </summary>
        /// <param name="pluginMetadata">插件元数据</param>
        /// <param name="hostVersion">主程序版本</param>
        /// <returns>兼容性检查结果</returns>
        public static CompatibilityResult CheckHostCompatibility(PluginMetadata pluginMetadata, Version hostVersion)
        {
            var result = new CompatibilityResult
            {
                IsCompatible = true,
                PluginId = pluginMetadata.Id,
                PluginName = pluginMetadata.Name,
                PluginVersion = pluginMetadata.Version,
                HostVersion = hostVersion
            };

            // 检查最低主机版本要求
            if (pluginMetadata.MinHostVersion != null)
            {
                if (hostVersion < pluginMetadata.MinHostVersion)
                {
                    result.IsCompatible = false;
                    result.ErrorMessage = $"主程序版本过低，需要 {pluginMetadata.MinHostVersion} 或更高版本，当前版本: {hostVersion}";
                    return result;
                }
            }

            // 检查最高主机版本要求
            if (pluginMetadata.MaxHostVersion != null)
            {
                if (hostVersion > pluginMetadata.MaxHostVersion)
                {
                    result.IsCompatible = false;
                    result.ErrorMessage = $"主程序版本过高，支持的最高版本: {pluginMetadata.MaxHostVersion}，当前版本: {hostVersion}";
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// 检查插件依赖的兼容性
        /// </summary>
        /// <param name="pluginMetadata">插件元数据</param>
        /// <param name="installedPlugins">已安装的插件</param>
        /// <returns>依赖兼容性检查结果</returns>
        public static DependencyCompatibilityResult CheckDependencyCompatibility(
            PluginMetadata pluginMetadata,
            IEnumerable<InstalledPluginInfo> installedPlugins)
        {
            var result = new DependencyCompatibilityResult
            {
                IsCompatible = true,
                PluginId = pluginMetadata.Id,
                PluginName = pluginMetadata.Name,
                MissingDependencies = new List<PluginDependency>(),
                IncompatibleDependencies = new List<DependencyIssue>()
            };

            if (pluginMetadata.Dependencies == null || !pluginMetadata.Dependencies.Any())
            {
                return result;
            }

            var installedPluginsDict = installedPlugins.ToDictionary(p => p.Id, p => p);

            foreach (var dependency in pluginMetadata.Dependencies)
            {
                // 检查依赖是否存在
                if (!installedPluginsDict.TryGetValue(dependency.Name, out var installedPlugin))
                {
                    result.IsCompatible = false;
                    result.MissingDependencies.Add(dependency);
                    continue;
                }

                // 检查依赖版本是否满足要求
                if (installedPlugin.Version < dependency.MinVersion)
                {
                    result.IsCompatible = false;
                    result.IncompatibleDependencies.Add(new DependencyIssue
                    {
                        DependencyId = dependency.Name,
                        RequiredVersion = dependency.MinVersion ?? new Version(1, 0, 0),
                        InstalledVersion = installedPlugin.Version,
                        IssueType = DependencyIssueType.VersionTooLow
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 检查 .NET 版本兼容性
        /// </summary>
        /// <param name="requiredDotNetVersion">要求的 .NET 版本</param>
        /// <returns>.NET 版本兼容性检查结果</returns>
        public static DotNetCompatibilityResult CheckDotNetCompatibility(string requiredDotNetVersion)
        {
            var result = new DotNetCompatibilityResult
            {
                IsCompatible = true,
                RequiredVersion = requiredDotNetVersion,
                CurrentVersion = Environment.Version.ToString()
            };

            try
            {
                var currentVersion = Environment.Version;
                var requiredVersion = Version.Parse(requiredDotNetVersion);

                if (currentVersion < requiredVersion)
                {
                    result.IsCompatible = false;
                    result.ErrorMessage = $"需要 .NET {requiredDotNetVersion} 或更高版本，当前版本: {currentVersion}";
                }
            }
            catch (Exception ex)
            {
                result.IsCompatible = false;
                result.ErrorMessage = $"版本解析失败: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 检查操作系统兼容性
        /// </summary>
        /// <param name="requiredOS">要求的操作系统</param>
        /// <returns>操作系统兼容性检查结果</returns>
        public static OSCompatibilityResult CheckOSCompatibility(string requiredOS)
        {
            var result = new OSCompatibilityResult
            {
                IsCompatible = true,
                RequiredOS = requiredOS,
                CurrentOS = Environment.OSVersion.ToString()
            };

            // 这里可以实现更复杂的操作系统兼容性检查
            // 目前简单检查是否为 Windows
            if (!string.IsNullOrEmpty(requiredOS) && 
                requiredOS.ToLower().Contains("windows") && 
                !Environment.OSVersion.Platform.ToString().Contains("Win"))
            {
                result.IsCompatible = false;
                result.ErrorMessage = $"需要 Windows 操作系统，当前系统: {Environment.OSVersion.Platform}";
            }

            return result;
        }

        /// <summary>
        /// 综合兼容性检查
        /// </summary>
        /// <param name="pluginMetadata">插件元数据</param>
        /// <param name="hostVersion">主程序版本</param>
        /// <param name="installedPlugins">已安装的插件</param>
        /// <returns>综合兼容性检查结果</returns>
        public static ComprehensiveCompatibilityResult CheckComprehensiveCompatibility(
            PluginMetadata pluginMetadata,
            Version hostVersion,
            IEnumerable<InstalledPluginInfo> installedPlugins)
        {
            var result = new ComprehensiveCompatibilityResult
            {
                PluginId = pluginMetadata.Id,
                PluginName = pluginMetadata.Name,
                PluginVersion = pluginMetadata.Version
            };

            // 检查主机兼容性
            result.HostCompatibility = CheckHostCompatibility(pluginMetadata, hostVersion);
            if (!result.HostCompatibility.IsCompatible)
            {
                result.IsCompatible = false;
                result.ErrorMessages.Add($"主机兼容性: {result.HostCompatibility.ErrorMessage}");
            }

            // 检查依赖兼容性
            result.DependencyCompatibility = CheckDependencyCompatibility(pluginMetadata, installedPlugins);
            if (!result.DependencyCompatibility.IsCompatible)
            {
                result.IsCompatible = false;
                if (result.DependencyCompatibility.MissingDependencies.Any())
                {
                    var missing = string.Join(", ", result.DependencyCompatibility.MissingDependencies.Select(d => d.Id));
                    result.ErrorMessages.Add($"缺少依赖: {missing}");
                }
                if (result.DependencyCompatibility.IncompatibleDependencies.Any())
                {
                    var incompatible = string.Join(", ", result.DependencyCompatibility.IncompatibleDependencies.Select(d => $"{d.DependencyId} (需要 {d.RequiredVersion}，已安装 {d.InstalledVersion})"));
                    result.ErrorMessages.Add($"依赖版本不兼容: {incompatible}");
                }
            }

            // 检查 .NET 版本兼容性
            if (!string.IsNullOrEmpty(pluginMetadata.MinimumDotNetVersion))
            {
                result.DotNetCompatibility = CheckDotNetCompatibility(pluginMetadata.MinimumDotNetVersion);
                if (!result.DotNetCompatibility.IsCompatible)
                {
                    result.IsCompatible = false;
                    result.ErrorMessages.Add($"运行时兼容性: {result.DotNetCompatibility.ErrorMessage}");
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 兼容性检查结果
    /// </summary>
    public class CompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public string PluginId { get; set; } = string.Empty;
        public string PluginName { get; set; } = string.Empty;
        public Version PluginVersion { get; set; } = new Version(1, 0, 0);
        public Version HostVersion { get; set; } = new Version(1, 0, 0);
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 依赖兼容性检查结果
    /// </summary>
    public class DependencyCompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public string PluginId { get; set; } = string.Empty;
        public string PluginName { get; set; } = string.Empty;
        public List<PluginDependency> MissingDependencies { get; set; } = new();
        public List<DependencyIssue> IncompatibleDependencies { get; set; } = new();
    }

    /// <summary>
    /// 依赖问题
    /// </summary>
    public class DependencyIssue
    {
        public string DependencyId { get; set; } = string.Empty;
        public Version RequiredVersion { get; set; } = new Version(1, 0, 0);
        public Version InstalledVersion { get; set; } = new Version(1, 0, 0);
        public DependencyIssueType IssueType { get; set; }
    }

    /// <summary>
    /// 依赖问题类型
    /// </summary>
    public enum DependencyIssueType
    {
        /// <summary>
        /// 版本过低
        /// </summary>
        VersionTooLow,

        /// <summary>
        /// 版本过高
        /// </summary>
        VersionTooHigh,

        /// <summary>
        /// 依赖缺失
        /// </summary>
        Missing
    }

    /// <summary>
    /// .NET 兼容性检查结果
    /// </summary>
    public class DotNetCompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public string RequiredVersion { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 操作系统兼容性检查结果
    /// </summary>
    public class OSCompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public string RequiredOS { get; set; } = string.Empty;
        public string CurrentOS { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 综合兼容性检查结果
    /// </summary>
    public class ComprehensiveCompatibilityResult
    {
        public bool IsCompatible { get; set; } = true;
        public string PluginId { get; set; } = string.Empty;
        public string PluginName { get; set; } = string.Empty;
        public Version PluginVersion { get; set; } = new Version(1, 0, 0);
        public CompatibilityResult HostCompatibility { get; set; } = new();
        public DependencyCompatibilityResult DependencyCompatibility { get; set; } = new();
        public DotNetCompatibilityResult DotNetCompatibility { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();

        /// <summary>
        /// 获取详细的兼容性报告
        /// </summary>
        /// <returns>兼容性报告</returns>
        public string GetCompatibilityReport()
        {
            if (IsCompatible)
            {
                return $"插件 {PluginName} v{PluginVersion} 与当前环境兼容";
            }

            var report = $"插件 {PluginName} v{PluginVersion} 兼容性检查失败:\n";
            foreach (var error in ErrorMessages)
            {
                report += $"• {error}\n";
            }

            return report.TrimEnd('\n');
        }
    }
}
