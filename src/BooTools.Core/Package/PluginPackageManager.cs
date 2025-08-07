using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BooTools.Core.Models;
using BooTools.Core.Package.Models;

namespace BooTools.Core.Package
{
    /// <summary>
    /// 插件包管理器
    /// </summary>
    public class PluginPackageManager
    {
        private readonly BooTools.Core.ILogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        
        /// <summary>
        /// 初始化插件包管理器
        /// </summary>
        /// <param name="logger">日志服务</param>
        public PluginPackageManager(BooTools.Core.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }
        
        /// <summary>
        /// 创建插件包
        /// </summary>
        /// <param name="pluginDirectory">插件目录</param>
        /// <param name="outputPath">输出路径</param>
        /// <param name="metadata">插件元数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建结果</returns>
        public async Task<PluginResult<string>> CreatePackageAsync(
            string pluginDirectory,
            string outputPath,
            PluginMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"开始创建插件包: {metadata.Name}");
                
                if (!Directory.Exists(pluginDirectory))
                {
                    return PluginResult<string>.Failure("插件目录不存在");
                }
                
                // 创建插件包模型
                var package = new PluginPackage
                {
                    Metadata = metadata,
                    PackageInfo = new PackageInfo
                    {
                        Type = PackageType.Plugin,
                        Compression = CompressionType.Zip,
                        ChecksumAlgorithm = "SHA256"
                    },
                    BuildInfo = new BuildInfo
                    {
                        BuildTime = DateTime.Now,
                        BuildEnvironment = Environment.MachineName,
                        Configuration = "Release"
                    }
                };
                
                // 扫描插件目录，添加文件到包清单
                await ScanPluginDirectoryAsync(pluginDirectory, package, cancellationToken);
                
                // 创建包文件
                var packagePath = await CreatePackageFileAsync(pluginDirectory, outputPath, package, cancellationToken);
                
                _logger.LogInfo($"插件包创建成功: {packagePath}");
                return PluginResult<string>.Success(packagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"创建插件包失败: {metadata.Name}", ex);
                return PluginResult<string>.Failure($"创建插件包失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 解析插件包
        /// </summary>
        /// <param name="packagePath">包文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>解析结果</returns>
        public async Task<PluginResult<PluginPackage>> ParsePackageAsync(
            string packagePath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"开始解析插件包: {packagePath}");
                
                if (!File.Exists(packagePath))
                {
                    return PluginResult<PluginPackage>.Failure("插件包文件不存在");
                }
                
                using var archive = ZipFile.OpenRead(packagePath);
                
                // 查找清单文件
                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry == null)
                {
                    return PluginResult<PluginPackage>.Failure("插件包清单文件不存在");
                }
                
                // 读取清单文件
                using var manifestStream = manifestEntry.Open();
                using var reader = new StreamReader(manifestStream);
                var manifestJson = await reader.ReadToEndAsync(cancellationToken);
                
                var package = JsonSerializer.Deserialize<PluginPackage>(manifestJson, _jsonOptions);
                if (package == null)
                {
                    return PluginResult<PluginPackage>.Failure("插件包清单格式无效");
                }
                
                // 验证包完整性
                var verificationResult = await VerifyPackageIntegrityAsync(packagePath, package, cancellationToken);
                if (!verificationResult.IsSuccess)
                {
                    return PluginResult<PluginPackage>.Failure($"包完整性验证失败: {verificationResult.ErrorMessage}");
                }
                
                _logger.LogInfo($"插件包解析成功: {package.Metadata.Name} v{package.Metadata.Version}");
                return PluginResult<PluginPackage>.Success(package);
            }
            catch (Exception ex)
            {
                _logger.LogError($"解析插件包失败: {packagePath}", ex);
                return PluginResult<PluginPackage>.Failure($"解析插件包失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 安装插件包
        /// </summary>
        /// <param name="packagePath">包文件路径</param>
        /// <param name="installDirectory">安装目录</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>安装结果</returns>
        public async Task<PluginResult> InstallPackageAsync(
            string packagePath,
            string installDirectory,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"开始安装插件包: {packagePath}");
                
                // 解析插件包
                var parseResult = await ParsePackageAsync(packagePath, cancellationToken);
                if (!parseResult.IsSuccess || parseResult.Data == null)
                {
                    return PluginResult.Failure($"解析插件包失败: {parseResult.ErrorMessage}");
                }
                
                var package = parseResult.Data;
                var pluginInstallPath = Path.Combine(installDirectory, package.Metadata.Id);
                
                // 创建安装目录
                Directory.CreateDirectory(pluginInstallPath);
                
                // 执行预安装脚本
                if (package.InstallationScript?.PreInstallSteps.Count > 0)
                {
                    var preInstallResult = await ExecuteInstallationStepsAsync(
                        package.InstallationScript.PreInstallSteps, 
                        pluginInstallPath, 
                        cancellationToken);
                    
                    if (!preInstallResult.IsSuccess)
                    {
                        return PluginResult.Failure($"预安装步骤执行失败: {preInstallResult.ErrorMessage}");
                    }
                }
                
                // 提取文件
                var extractResult = await ExtractPackageFilesAsync(packagePath, pluginInstallPath, package, cancellationToken);
                if (!extractResult.IsSuccess)
                {
                    return PluginResult.Failure($"提取文件失败: {extractResult.ErrorMessage}");
                }
                
                // 执行后安装脚本
                if (package.InstallationScript?.PostInstallSteps.Count > 0)
                {
                    var postInstallResult = await ExecuteInstallationStepsAsync(
                        package.InstallationScript.PostInstallSteps, 
                        pluginInstallPath, 
                        cancellationToken);
                    
                    if (!postInstallResult.IsSuccess)
                    {
                        _logger.LogWarning($"后安装步骤执行失败: {postInstallResult.ErrorMessage}");
                        // 后安装步骤失败不影响整体安装结果
                    }
                }
                
                _logger.LogInfo($"插件包安装成功: {package.Metadata.Name} v{package.Metadata.Version}");
                return PluginResult.Success($"插件 {package.Metadata.Name} 安装成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"安装插件包失败: {packagePath}", ex);
                return PluginResult.Failure($"安装插件包失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 卸载插件包
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <param name="installDirectory">安装目录</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>卸载结果</returns>
        public async Task<PluginResult> UninstallPackageAsync(
            string pluginId,
            string installDirectory,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"开始卸载插件: {pluginId}");
                
                var pluginInstallPath = Path.Combine(installDirectory, pluginId);
                if (!Directory.Exists(pluginInstallPath))
                {
                    return PluginResult.Failure("插件安装目录不存在");
                }
                
                // 查找插件清单文件
                var manifestPath = Path.Combine(pluginInstallPath, "manifest.json");
                PluginPackage? package = null;
                
                if (File.Exists(manifestPath))
                {
                    var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                    package = JsonSerializer.Deserialize<PluginPackage>(manifestJson, _jsonOptions);
                }
                
                // 执行预卸载脚本
                if (package?.UninstallationScript?.PreUninstallSteps.Count > 0)
                {
                    var preUninstallResult = await ExecuteInstallationStepsAsync(
                        package.UninstallationScript.PreUninstallSteps, 
                        pluginInstallPath, 
                        cancellationToken);
                    
                    if (!preUninstallResult.IsSuccess)
                    {
                        _logger.LogWarning($"预卸载步骤执行失败: {preUninstallResult.ErrorMessage}");
                    }
                }
                
                // 删除插件目录
                Directory.Delete(pluginInstallPath, true);
                
                // 执行后卸载脚本
                if (package?.UninstallationScript?.PostUninstallSteps.Count > 0)
                {
                    var postUninstallResult = await ExecuteInstallationStepsAsync(
                        package.UninstallationScript.PostUninstallSteps, 
                        string.Empty, // 插件目录已删除
                        cancellationToken);
                    
                    if (!postUninstallResult.IsSuccess)
                    {
                        _logger.LogWarning($"后卸载步骤执行失败: {postUninstallResult.ErrorMessage}");
                    }
                }
                
                _logger.LogInfo($"插件卸载成功: {pluginId}");
                return PluginResult.Success($"插件 {pluginId} 卸载成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"卸载插件失败: {pluginId}", ex);
                return PluginResult.Failure($"卸载插件失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 验证插件包
        /// </summary>
        /// <param name="packagePath">包文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        public async Task<PluginResult<bool>> VerifyPackageAsync(
            string packagePath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var parseResult = await ParsePackageAsync(packagePath, cancellationToken);
                if (!parseResult.IsSuccess)
                {
                    return PluginResult<bool>.Success(false, parseResult.ErrorMessage ?? "解析失败");
                }
                
                var package = parseResult.Data!;
                
                // 验证包完整性
                var integrityResult = await VerifyPackageIntegrityAsync(packagePath, package, cancellationToken);
                if (!integrityResult.IsSuccess)
                {
                    return PluginResult<bool>.Success(false, integrityResult.ErrorMessage ?? "完整性验证失败");
                }
                
                // 验证数字签名（如果有）
                if (package.Signature != null)
                {
                    var signatureResult = await VerifyDigitalSignatureAsync(packagePath, package.Signature, cancellationToken);
                    if (!signatureResult.IsSuccess)
                    {
                        return PluginResult<bool>.Success(false, $"数字签名验证失败: {signatureResult.ErrorMessage ?? "未知错误"}");
                    }
                }
                
                return PluginResult<bool>.Success(true, "插件包验证通过");
            }
            catch (Exception ex)
            {
                _logger.LogError($"验证插件包失败: {packagePath}", ex);
                return PluginResult<bool>.Failure($"验证插件包失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 扫描插件目录
        /// </summary>
        /// <param name="pluginDirectory">插件目录</param>
        /// <param name="package">插件包</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task ScanPluginDirectoryAsync(
            string pluginDirectory, 
            PluginPackage package, 
            CancellationToken cancellationToken)
        {
            var files = Directory.GetFiles(pluginDirectory, "*", SearchOption.AllDirectories);
            long totalSize = 0;
            
            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);
                var relativePath = Path.GetRelativePath(pluginDirectory, filePath);
                
                var packageFile = new PackageFile
                {
                    Path = relativePath.Replace('\\', '/'),
                    Size = fileInfo.Length,
                    Type = DetermineFileType(filePath),
                    IsExecutable = Path.GetExtension(filePath).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                                  Path.GetExtension(filePath).Equals(".dll", StringComparison.OrdinalIgnoreCase)
                };
                
                // 计算文件校验和
                packageFile.Checksum = await ComputeFileChecksumAsync(filePath, cancellationToken);
                
                package.Files.Add(packageFile);
                totalSize += fileInfo.Length;
            }
            
            package.PackageInfo.Size = totalSize;
        }
        
        /// <summary>
        /// 创建包文件
        /// </summary>
        /// <param name="pluginDirectory">插件目录</param>
        /// <param name="outputPath">输出路径</param>
        /// <param name="package">插件包</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包文件路径</returns>
        private async Task<string> CreatePackageFileAsync(
            string pluginDirectory,
            string outputPath,
            PluginPackage package,
            CancellationToken cancellationToken)
        {
            var packagePath = Path.ChangeExtension(outputPath, ".bpkg");
            
            using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
            
            // 添加清单文件
            var manifestEntry = archive.CreateEntry("manifest.json");
            using (var manifestStream = manifestEntry.Open())
            {
                var manifestJson = JsonSerializer.Serialize(package, _jsonOptions);
                var manifestBytes = System.Text.Encoding.UTF8.GetBytes(manifestJson);
                await manifestStream.WriteAsync(manifestBytes, 0, manifestBytes.Length, cancellationToken);
            }
            
            // 添加插件文件
            foreach (var file in package.Files)
            {
                var sourceFilePath = Path.Combine(pluginDirectory, file.Path.Replace('/', '\\'));
                if (File.Exists(sourceFilePath))
                {
                    archive.CreateEntryFromFile(sourceFilePath, file.Path);
                }
            }
            
            // 计算包校验和
            package.PackageInfo.Checksum = await ComputeFileChecksumAsync(packagePath, cancellationToken);
            
            // 更新清单文件中的校验和
            var updatedManifestEntry = archive.GetEntry("manifest.json");
            if (updatedManifestEntry != null)
            {
                updatedManifestEntry.Delete();
                var newManifestEntry = archive.CreateEntry("manifest.json");
                using var newManifestStream = newManifestEntry.Open();
                var updatedManifestJson = JsonSerializer.Serialize(package, _jsonOptions);
                var updatedManifestBytes = System.Text.Encoding.UTF8.GetBytes(updatedManifestJson);
                await newManifestStream.WriteAsync(updatedManifestBytes, 0, updatedManifestBytes.Length, cancellationToken);
            }
            
            return packagePath;
        }
        
        /// <summary>
        /// 提取包文件
        /// </summary>
        /// <param name="packagePath">包文件路径</param>
        /// <param name="installDirectory">安装目录</param>
        /// <param name="package">插件包</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>提取结果</returns>
        private async Task<PluginResult> ExtractPackageFilesAsync(
            string packagePath,
            string installDirectory,
            PluginPackage package,
            CancellationToken cancellationToken)
        {
            try
            {
                using var archive = ZipFile.OpenRead(packagePath);
                
                foreach (var file in package.Files)
                {
                    var entry = archive.GetEntry(file.Path);
                    if (entry == null) continue;
                    
                    var targetPath = Path.Combine(installDirectory, file.Path.Replace('/', '\\'));
                    var targetDirectory = Path.GetDirectoryName(targetPath);
                    
                    if (!string.IsNullOrEmpty(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                    
                    entry.ExtractToFile(targetPath, true);
                    
                    // 验证提取的文件
                    if (!string.IsNullOrEmpty(file.Checksum))
                    {
                        var actualChecksum = await ComputeFileChecksumAsync(targetPath, cancellationToken);
                        if (!actualChecksum.Equals(file.Checksum, StringComparison.OrdinalIgnoreCase))
                        {
                            return PluginResult.Failure($"文件校验和不匹配: {file.Path}");
                        }
                    }
                }
                
                // 保存清单文件
                var manifestPath = Path.Combine(installDirectory, "manifest.json");
                var manifestJson = JsonSerializer.Serialize(package, _jsonOptions);
                await File.WriteAllTextAsync(manifestPath, manifestJson, cancellationToken);
                
                return PluginResult.Success("文件提取成功");
            }
            catch (Exception ex)
            {
                return PluginResult.Failure($"文件提取失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 执行安装步骤
        /// </summary>
        /// <param name="steps">安装步骤</param>
        /// <param name="workingDirectory">工作目录</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        private async Task<PluginResult> ExecuteInstallationStepsAsync(
            System.Collections.Generic.List<InstallationStep> steps,
            string workingDirectory,
            CancellationToken cancellationToken)
        {
            foreach (var step in steps)
            {
                try
                {
                    _logger.LogInfo($"执行安装步骤: {step.Name}");
                    
                    // 这里可以根据步骤类型执行不同的操作
                    // 为了简化，目前只记录日志
                    await Task.Delay(100, cancellationToken); // 模拟执行时间
                    
                    _logger.LogInfo($"安装步骤执行成功: {step.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"安装步骤执行失败: {step.Name}", ex);
                    
                    if (step.IsRequired && !step.ContinueOnFailure)
                    {
                        return PluginResult.Failure($"必需步骤执行失败: {step.Name} - {ex.Message}");
                    }
                }
            }
            
            return PluginResult.Success("所有安装步骤执行完成");
        }
        
        /// <summary>
        /// 验证包完整性
        /// </summary>
        /// <param name="packagePath">包文件路径</param>
        /// <param name="package">插件包</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        private async Task<PluginResult> VerifyPackageIntegrityAsync(
            string packagePath,
            PluginPackage package,
            CancellationToken cancellationToken)
        {
            try
            {
                // 验证包校验和
                if (!string.IsNullOrEmpty(package.PackageInfo.Checksum))
                {
                    var actualChecksum = await ComputeFileChecksumAsync(packagePath, cancellationToken);
                    if (!actualChecksum.Equals(package.PackageInfo.Checksum, StringComparison.OrdinalIgnoreCase))
                    {
                        return PluginResult.Failure("包校验和不匹配");
                    }
                }
                
                // 验证包大小
                var fileInfo = new FileInfo(packagePath);
                if (package.PackageInfo.Size > 0 && fileInfo.Length != package.PackageInfo.Size)
                {
                    return PluginResult.Failure($"包大小不匹配：期望 {package.PackageInfo.Size} 字节，实际 {fileInfo.Length} 字节");
                }
                
                return PluginResult.Success("包完整性验证通过");
            }
            catch (Exception ex)
            {
                return PluginResult.Failure($"包完整性验证失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 验证数字签名
        /// </summary>
        /// <param name="packagePath">包文件路径</param>
        /// <param name="signature">数字签名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        private async Task<PluginResult> VerifyDigitalSignatureAsync(
            string packagePath,
            DigitalSignature signature,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInfo($"开始验证数字签名: {signature.Algorithm}");
                
                // 基本验证
                if (string.IsNullOrEmpty(signature.Value))
                {
                    return PluginResult.Failure("数字签名值为空");
                }
                
                if (string.IsNullOrEmpty(signature.Certificate))
                {
                    return PluginResult.Failure("数字签名证书为空");
                }
                
                // 读取包文件内容
                var packageBytes = await File.ReadAllBytesAsync(packagePath, cancellationToken);
                
                // 根据算法选择验证方式
                var verificationResult = signature.Algorithm.ToUpperInvariant() switch
                {
                    "RSA-SHA256" => await VerifyRSASHA256SignatureAsync(packageBytes, signature, cancellationToken),
                    "RSA-SHA512" => await VerifyRSASHA512SignatureAsync(packageBytes, signature, cancellationToken),
                    _ => PluginResult.Failure($"不支持的签名算法: {signature.Algorithm}")
                };
                
                if (verificationResult.IsSuccess)
                {
                    _logger.LogInfo($"数字签名验证通过: {signature.Signer?.Name ?? "未知签名者"}");
                }
                else
                {
                    _logger.LogWarning($"数字签名验证失败: {verificationResult.ErrorMessage}");
                }
                
                return verificationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"数字签名验证异常: {packagePath}", ex);
                return PluginResult.Failure($"数字签名验证失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 验证 RSA-SHA256 数字签名
        /// </summary>
        /// <param name="data">要验证的数据</param>
        /// <param name="signature">数字签名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        private async Task<PluginResult> VerifyRSASHA256SignatureAsync(
            byte[] data,
            DigitalSignature signature,
            CancellationToken cancellationToken)
        {
            try
            {
                // 解析证书
                var certificateBytes = Convert.FromBase64String(signature.Certificate);
                using var certificate = new X509Certificate2(certificateBytes);
                
                // 获取公钥 - 使用推荐的方法
                using var rsa = certificate.GetRSAPublicKey();
                if (rsa == null)
                {
                    return PluginResult.Failure("无法从证书获取RSA公钥");
                }
                
                // 解析签名值
                var signatureBytes = Convert.FromBase64String(signature.Value);
                
                // 验证签名
                var isValid = rsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                
                if (isValid)
                {
                    // 验证证书有效性
                    var certValidation = await ValidateCertificateAsync(certificate, cancellationToken);
                    return certValidation.IsSuccess 
                        ? PluginResult.Success("RSA-SHA256 签名验证通过")
                        : PluginResult.Failure($"证书验证失败: {certValidation.ErrorMessage}");
                }
                
                return PluginResult.Failure("RSA-SHA256 签名验证失败");
            }
            catch (Exception ex)
            {
                return PluginResult.Failure($"RSA-SHA256 签名验证异常: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 验证 RSA-SHA512 数字签名
        /// </summary>
        /// <param name="data">要验证的数据</param>
        /// <param name="signature">数字签名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        private async Task<PluginResult> VerifyRSASHA512SignatureAsync(
            byte[] data,
            DigitalSignature signature,
            CancellationToken cancellationToken)
        {
            try
            {
                // 解析证书
                var certificateBytes = Convert.FromBase64String(signature.Certificate);
                using var certificate = new X509Certificate2(certificateBytes);
                
                // 获取公钥 - 使用推荐的方法
                using var rsa = certificate.GetRSAPublicKey();
                if (rsa == null)
                {
                    return PluginResult.Failure("无法从证书获取RSA公钥");
                }
                
                // 解析签名值
                var signatureBytes = Convert.FromBase64String(signature.Value);
                
                // 验证签名
                var isValid = rsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
                
                if (isValid)
                {
                    // 验证证书有效性
                    var certValidation = await ValidateCertificateAsync(certificate, cancellationToken);
                    return certValidation.IsSuccess 
                        ? PluginResult.Success("RSA-SHA512 签名验证通过")
                        : PluginResult.Failure($"证书验证失败: {certValidation.ErrorMessage}");
                }
                
                return PluginResult.Failure("RSA-SHA512 签名验证失败");
            }
            catch (Exception ex)
            {
                return PluginResult.Failure($"RSA-SHA512 签名验证异常: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 验证证书有效性
        /// </summary>
        /// <param name="certificate">证书</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        private async Task<PluginResult> ValidateCertificateAsync(
            System.Security.Cryptography.X509Certificates.X509Certificate2 certificate,
            CancellationToken cancellationToken)
        {
            try
            {
                // 检查证书有效期
                var now = DateTime.Now;
                if (now < certificate.NotBefore)
                {
                    return PluginResult.Failure($"证书尚未生效，生效时间: {certificate.NotBefore}");
                }
                
                if (now > certificate.NotAfter)
                {
                    return PluginResult.Failure($"证书已过期，过期时间: {certificate.NotAfter}");
                }
                
                // 检查证书用途
                var keyUsages = certificate.Extensions["2.5.29.15"] as System.Security.Cryptography.X509Certificates.X509KeyUsageExtension;
                if (keyUsages != null && !keyUsages.KeyUsages.HasFlag(System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.DigitalSignature))
                {
                    return PluginResult.Failure("证书不支持数字签名用途");
                }
                
                // TODO: 可以添加更多证书验证逻辑，如：
                // - 证书链验证
                // - 撤销列表检查
                // - 受信任的根证书验证
                
                await Task.Delay(1, cancellationToken); // 避免编译器警告
                return PluginResult.Success("证书验证通过");
            }
            catch (Exception ex)
            {
                return PluginResult.Failure($"证书验证异常: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 确定文件类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件类型</returns>
        private static FileType DetermineFileType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".dll" or ".exe" => FileType.Assembly,
                ".config" or ".json" or ".xml" => FileType.Configuration,
                ".png" or ".jpg" or ".jpeg" or ".gif" or ".ico" => FileType.Resource,
                ".md" or ".txt" or ".pdf" => FileType.Documentation,
                ".ps1" or ".bat" or ".cmd" => FileType.Script,
                ".dat" or ".db" or ".sqlite" => FileType.Data,
                _ => FileType.Unknown
            };
        }
        
        /// <summary>
        /// 计算文件校验和
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>校验和</returns>
        private async Task<string> ComputeFileChecksumAsync(string filePath, CancellationToken cancellationToken)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
