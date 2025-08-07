using System;
using System.Collections.Generic;
using BooTools.Core.Models;

namespace BooTools.Core.Package.Models
{
    /// <summary>
    /// 插件包模型
    /// </summary>
    public class PluginPackage
    {
        /// <summary>
        /// 包格式版本
        /// </summary>
        public string FormatVersion { get; set; } = "1.0";
        
        /// <summary>
        /// 插件元数据
        /// </summary>
        public PluginMetadata Metadata { get; set; } = new();
        
        /// <summary>
        /// 包信息
        /// </summary>
        public PackageInfo PackageInfo { get; set; } = new();
        
        /// <summary>
        /// 文件清单
        /// </summary>
        public List<PackageFile> Files { get; set; } = new();
        
        /// <summary>
        /// 安装脚本
        /// </summary>
        public InstallationScript? InstallationScript { get; set; }
        
        /// <summary>
        /// 卸载脚本
        /// </summary>
        public UninstallationScript? UninstallationScript { get; set; }
        
        /// <summary>
        /// 许可证信息
        /// </summary>
        public LicenseInfo? License { get; set; }
        
        /// <summary>
        /// 数字签名
        /// </summary>
        public DigitalSignature? Signature { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 构建信息
        /// </summary>
        public BuildInfo? BuildInfo { get; set; }
    }
    
    /// <summary>
    /// 包信息
    /// </summary>
    public class PackageInfo
    {
        /// <summary>
        /// 包大小（字节）
        /// </summary>
        public long Size { get; set; } = 0;
        
        /// <summary>
        /// 压缩算法
        /// </summary>
        public CompressionType Compression { get; set; } = CompressionType.Zip;
        
        /// <summary>
        /// 校验和
        /// </summary>
        public string Checksum { get; set; } = string.Empty;
        
        /// <summary>
        /// 校验和算法
        /// </summary>
        public string ChecksumAlgorithm { get; set; } = "SHA256";
        
        /// <summary>
        /// 加密类型
        /// </summary>
        public EncryptionType Encryption { get; set; } = EncryptionType.None;
        
        /// <summary>
        /// 包类型
        /// </summary>
        public PackageType Type { get; set; } = PackageType.Plugin;
    }
    
    /// <summary>
    /// 包文件信息
    /// </summary>
    public class PackageFile
    {
        /// <summary>
        /// 文件路径（包内相对路径）
        /// </summary>
        public string Path { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件大小
        /// </summary>
        public long Size { get; set; } = 0;
        
        /// <summary>
        /// 文件校验和
        /// </summary>
        public string Checksum { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件类型
        /// </summary>
        public FileType Type { get; set; } = FileType.Unknown;
        
        /// <summary>
        /// 安装目标路径
        /// </summary>
        public string? TargetPath { get; set; }
        
        /// <summary>
        /// 是否为可执行文件
        /// </summary>
        public bool IsExecutable { get; set; } = false;
        
        /// <summary>
        /// 文件权限
        /// </summary>
        public FilePermissions Permissions { get; set; } = FilePermissions.ReadWrite;
        
        /// <summary>
        /// 文件描述
        /// </summary>
        public string? Description { get; set; }
    }
    
    /// <summary>
    /// 安装脚本
    /// </summary>
    public class InstallationScript
    {
        /// <summary>
        /// 脚本类型
        /// </summary>
        public ScriptType Type { get; set; } = ScriptType.PowerShell;
        
        /// <summary>
        /// 脚本内容
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 预安装步骤
        /// </summary>
        public List<InstallationStep> PreInstallSteps { get; set; } = new();
        
        /// <summary>
        /// 后安装步骤
        /// </summary>
        public List<InstallationStep> PostInstallSteps { get; set; } = new();
        
        /// <summary>
        /// 所需权限
        /// </summary>
        public List<string> RequiredPermissions { get; set; } = new();
    }
    
    /// <summary>
    /// 卸载脚本
    /// </summary>
    public class UninstallationScript
    {
        /// <summary>
        /// 脚本类型
        /// </summary>
        public ScriptType Type { get; set; } = ScriptType.PowerShell;
        
        /// <summary>
        /// 脚本内容
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 预卸载步骤
        /// </summary>
        public List<InstallationStep> PreUninstallSteps { get; set; } = new();
        
        /// <summary>
        /// 后卸载步骤
        /// </summary>
        public List<InstallationStep> PostUninstallSteps { get; set; } = new();
        
        /// <summary>
        /// 是否清理用户数据
        /// </summary>
        public bool CleanUserData { get; set; } = false;
    }
    
    /// <summary>
    /// 安装步骤
    /// </summary>
    public class InstallationStep
    {
        /// <summary>
        /// 步骤名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 步骤描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 步骤类型
        /// </summary>
        public StepType Type { get; set; } = StepType.Custom;
        
        /// <summary>
        /// 步骤参数
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new();
        
        /// <summary>
        /// 是否为必需步骤
        /// </summary>
        public bool IsRequired { get; set; } = true;
        
        /// <summary>
        /// 失败时是否继续
        /// </summary>
        public bool ContinueOnFailure { get; set; } = false;
    }
    
    /// <summary>
    /// 许可证信息
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>
        /// 许可证类型
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// 许可证文本
        /// </summary>
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// 许可证URL
        /// </summary>
        public string? Url { get; set; }
        
        /// <summary>
        /// 是否需要用户接受
        /// </summary>
        public bool RequiresAcceptance { get; set; } = false;
    }
    
    /// <summary>
    /// 数字签名
    /// </summary>
    public class DigitalSignature
    {
        /// <summary>
        /// 签名算法
        /// </summary>
        public string Algorithm { get; set; } = "RSA-SHA256";
        
        /// <summary>
        /// 签名值
        /// </summary>
        public string Value { get; set; } = string.Empty;
        
        /// <summary>
        /// 签名证书
        /// </summary>
        public string Certificate { get; set; } = string.Empty;
        
        /// <summary>
        /// 签名时间
        /// </summary>
        public DateTime SignedTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 签名者信息
        /// </summary>
        public SignerInfo? Signer { get; set; }
    }
    
    /// <summary>
    /// 签名者信息
    /// </summary>
    public class SignerInfo
    {
        /// <summary>
        /// 签名者名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 签名者邮箱
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// 组织名称
        /// </summary>
        public string? Organization { get; set; }
        
        /// <summary>
        /// 证书指纹
        /// </summary>
        public string CertificateThumbprint { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 构建信息
    /// </summary>
    public class BuildInfo
    {
        /// <summary>
        /// 构建版本
        /// </summary>
        public string BuildVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// 构建时间
        /// </summary>
        public DateTime BuildTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 构建环境
        /// </summary>
        public string BuildEnvironment { get; set; } = string.Empty;
        
        /// <summary>
        /// 构建工具
        /// </summary>
        public string BuildTool { get; set; } = "BooTools PackageBuilder";
        
        /// <summary>
        /// 源代码版本
        /// </summary>
        public string? SourceVersion { get; set; }
        
        /// <summary>
        /// 构建配置
        /// </summary>
        public string Configuration { get; set; } = "Release";
    }
    
    /// <summary>
    /// 压缩类型枚举
    /// </summary>
    public enum CompressionType
    {
        /// <summary>
        /// 无压缩
        /// </summary>
        None,
        
        /// <summary>
        /// ZIP压缩
        /// </summary>
        Zip,
        
        /// <summary>
        /// GZip压缩
        /// </summary>
        GZip,
        
        /// <summary>
        /// 7-Zip压缩
        /// </summary>
        SevenZip
    }
    
    /// <summary>
    /// 加密类型枚举
    /// </summary>
    public enum EncryptionType
    {
        /// <summary>
        /// 无加密
        /// </summary>
        None,
        
        /// <summary>
        /// AES加密
        /// </summary>
        AES,
        
        /// <summary>
        /// RSA加密
        /// </summary>
        RSA
    }
    
    /// <summary>
    /// 包类型枚举
    /// </summary>
    public enum PackageType
    {
        /// <summary>
        /// 插件包
        /// </summary>
        Plugin,
        
        /// <summary>
        /// 主题包
        /// </summary>
        Theme,
        
        /// <summary>
        /// 语言包
        /// </summary>
        Language,
        
        /// <summary>
        /// 更新包
        /// </summary>
        Update
    }
    
    /// <summary>
    /// 文件类型枚举
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// 未知类型
        /// </summary>
        Unknown,
        
        /// <summary>
        /// 程序集
        /// </summary>
        Assembly,
        
        /// <summary>
        /// 配置文件
        /// </summary>
        Configuration,
        
        /// <summary>
        /// 资源文件
        /// </summary>
        Resource,
        
        /// <summary>
        /// 文档文件
        /// </summary>
        Documentation,
        
        /// <summary>
        /// 脚本文件
        /// </summary>
        Script,
        
        /// <summary>
        /// 数据文件
        /// </summary>
        Data
    }
    
    /// <summary>
    /// 文件权限枚举
    /// </summary>
    [Flags]
    public enum FilePermissions
    {
        /// <summary>
        /// 无权限
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 读权限
        /// </summary>
        Read = 1,
        
        /// <summary>
        /// 写权限
        /// </summary>
        Write = 2,
        
        /// <summary>
        /// 执行权限
        /// </summary>
        Execute = 4,
        
        /// <summary>
        /// 读写权限
        /// </summary>
        ReadWrite = Read | Write,
        
        /// <summary>
        /// 读执行权限
        /// </summary>
        ReadExecute = Read | Execute,
        
        /// <summary>
        /// 完全权限
        /// </summary>
        Full = Read | Write | Execute
    }
    
    /// <summary>
    /// 脚本类型枚举
    /// </summary>
    public enum ScriptType
    {
        /// <summary>
        /// PowerShell脚本
        /// </summary>
        PowerShell,
        
        /// <summary>
        /// 批处理脚本
        /// </summary>
        Batch,
        
        /// <summary>
        /// C#脚本
        /// </summary>
        CSharp,
        
        /// <summary>
        /// Python脚本
        /// </summary>
        Python
    }
    
    /// <summary>
    /// 安装步骤类型枚举
    /// </summary>
    public enum StepType
    {
        /// <summary>
        /// 自定义步骤
        /// </summary>
        Custom,
        
        /// <summary>
        /// 复制文件
        /// </summary>
        CopyFile,
        
        /// <summary>
        /// 创建目录
        /// </summary>
        CreateDirectory,
        
        /// <summary>
        /// 注册服务
        /// </summary>
        RegisterService,
        
        /// <summary>
        /// 设置权限
        /// </summary>
        SetPermissions,
        
        /// <summary>
        /// 运行命令
        /// </summary>
        RunCommand,
        
        /// <summary>
        /// 修改注册表
        /// </summary>
        ModifyRegistry
    }
}
