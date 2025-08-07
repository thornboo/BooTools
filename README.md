# BooTools - A Lightweight Plugin Toolbox for Windows

BooTools is a lightweight, extensible toolbox for Windows. It acts as a simple launcher, where all features are provided by independent plugins that can be installed, updated, and managed remotely.

## Core Features

- **Plugin-Powered**: All functionality comes from plugins.
- **Hot-Swapping**: Install, uninstall, and update plugins without restarting the application.
- **Plugin Store**: An easy-to-use store to browse, install, and manage plugins.
- **Secure**: Ensures plugin integrity and security through digital signatures.
- **High DPI Support**: A crisp and clear interface on any high-resolution display.

## Architecture Overview

BooTools uses a modern plugin architecture to ensure stability and flexibility:

- **Isolation**: Each plugin runs in its own `AssemblyLoadContext`, preventing dependency conflicts and enabling hot-swapping.
- **Remote Management**: A centralized system handles discovering, downloading, and updating plugins from remote repositories (like GitHub or a web server).
- **Standardized Packaging**: Plugins are packaged in a `.bpkg` format (a ZIP archive), which includes the plugin's code, assets, and a manifest file for metadata.

For more technical details, please see the [Architecture Documentation](./docs/项目架构.md).

## Installation and Usage

### System Requirements
- Windows 10/11
- .NET 8.0 Runtime

### Build and Run Steps

1.  **Clone the project**
    ```bash
    git clone <repository-url>
    cd boo-tools
    ```

2.  **Build the project**
    ```bash
    # Recommended: use the build script
    .\build.bat
    
    # Or build manually
    dotnet build BooTools.sln -c Release
    ```

3.  **Run the application**
    The executable is located at: `src\BooTools.UI\bin\Release\net8.0-windows\BooTools.UI.exe`.

## How to Contribute

Contributions via Issues and Pull Requests are welcome!

- **Create a Plugin**: Create a .NET Class Library project that implements the `BooTools.Core.Interfaces.IPlugin` interface. Place the compiled DLL in the `Plugins` directory to get started.
- **Debugging**: Logs are available in the console and the `bin/BooTools.UI/logs/` directory. You can also use the real-time log viewer from the "Debug" menu.

### Tech Stack
- **Language**: C# (.NET 8)
- **UI Framework**: Windows Forms (WinForms)
- **Core Dependency**: `Microsoft.Extensions.DependencyInjection`

## License 

This project is licensed under the [MIT License](LICENSE).

## Changelog

All notable changes to this project are documented in the [CHANGELOG.md](CHANGELOG.md) file.

