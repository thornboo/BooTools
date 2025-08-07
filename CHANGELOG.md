# Changelog

## [Unreleased]

### Added
- Full support for High DPI, ensuring a clear and well-proportioned interface on high-resolution screens.

### Changed
- **Plugin Standardization**: Refactored the `EnvironmentVariableEditor` plugin according to the *Plugin Development Guide*, unifying the design specifications. (`e824b7e`)
- Optimized the layout of windows and controls to be adaptive to different window sizes.

### Fixed
- Resolved all compile-time errors and warnings, improving code quality and stability.
- Unified the system tray icon to prevent multiple instances from appearing.
- Unified UI text localization to ensure language consistency.

## [2.0.0] - 2025-08-07

### Added
- **Remote Plugin System**:
  - Implemented a plugin discovery, download, installation, and update system supporting multiple repositories.
  - Defined the `.bpkg` plugin package format for standardized distribution.
  - Introduced a security framework based on digital signatures to ensure plugin source reliability.
- **Plugin Store**: Developed a graphical plugin store to simplify finding, installing, and managing plugins for users.

## [1.0.0] - 2025-08-06

### Added
- **Modern Plugin Architecture**:
  - Introduced `AssemblyLoadContext` for plugin isolation, supporting hot-swapping and dependency management.
  - Designed a standardized `IPlugin` interface and plugin lifecycle.
- **Core Service Decoupling**:
  - Introduced Dependency Injection (DI) to manage core services.
- **Backward Compatibility**:
  - Provided an adapter layer to ensure compatibility with legacy plugins.