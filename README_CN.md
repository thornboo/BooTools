# BooTools - 轻量级 Windows 插件工具箱

BooTools 是一个轻量级、可扩展的 Windows 工具箱。它本身是一个纯粹的启动器，所有功能都由独立的插件提供，而这些插件可以被远程安装、更新和管理。

## 核心特性

- **插件驱动**: 所有功能均来自插件。
- **热插拔**: 无需重启应用即可安装、卸载和更新插件。
- **插件商店**: 内置简单易用的插件商店，轻松浏览、安装和管理插件。
- **安全可靠**: 通过数字签名确保插件的完整性与安全性。
- **高 DPI 支持**: 在任何高分屏上都能获得清晰锐利的界面。

## 架构概览

BooTools 采用现代化的插件架构，以确保稳定性与灵活性：

- **隔离机制**: 每个插件都在独立的 `AssemblyLoadContext` 中运行，避免了依赖冲突，并实现了热插拔。
- **远程管理**: 一套集中的管理系统，负责从远程仓库（如 GitHub 或 Web 服务器）发现、下载和更新插件。
- **标准化打包**: 插件被打包为 `.bpkg` 格式（一个ZIP压缩包），其中包含插件代码、资源及其元数据清单。

更多技术细节，请参阅 [架构文档](./docs/项目架构.md)。

## 安装与运行

### 系统要求
- Windows 10/11
- .NET 8.0 Runtime

### 构建与运行

1.  **克隆项目**
    ```bash
    git clone <repository-url>
    cd boo-tools
    ```

2.  **构建项目**
    ```bash
    # 推荐使用构建脚本
    .\build.bat
    
    # 或手动构建
    dotnet build BooTools.sln -c Release
    ```

3.  **运行程序**
    可执行文件位于 `src\BooTools.UI\bin\Release\net8.0-windows\` 目录中。

## 如何贡献

欢迎通过提交 Issue 和 Pull Request 为项目做出贡献！

- **创建插件**: 创建一个实现 `BooTools.Core.Interfaces.IPlugin` 接口的 .NET 类库项目，并将编译后的 DLL 放入 `Plugins` 目录即可开始。
- **调试指南**: 日志会输出到控制台和 `bin/BooTools.UI/logs/` 目录。你也可以从主程序的“调试”菜单打开实时日志查看器。

### 技术栈
- **语言**: C# (.NET 8)
- **UI 框架**: Windows Forms (WinForms)
- **核心依赖**: `Microsoft.Extensions.DependencyInjection`

## 许可证

本项目采用 [MIT 许可证](LICENSE)。