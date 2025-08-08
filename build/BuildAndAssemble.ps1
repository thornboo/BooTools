# Build and Assemble Script for BooTools
param(
    [string]$Configuration = "Release",
    [string]$Framework = "net8.0-windows"
)

# Configuration
$RootOutputDir = "./bin"
$UIProjectDir = "./src/BooTools.UI"
$PluginsBaseDir = "./src/BooTools.Plugins"

# Clean previous build
Write-Host "Cleaning up old build artifacts..."
if (Test-Path $RootOutputDir) {
    Remove-Item -Path $RootOutputDir -Recurse -Force
}

# Build solution
Write-Host "Building the entire solution..."
dotnet build BooTools.sln -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Solution build failed!"
    exit 1
}

# Assemble files
Write-Host "Assembling application components..."

# Create output directories
New-Item -ItemType Directory -Path $RootOutputDir -Force | Out-Null
$PluginsDestDir = Join-Path $RootOutputDir "Plugins"
New-Item -ItemType Directory -Path $PluginsDestDir -Force | Out-Null

# Copy main application
Write-Host "Copying main UI application..."
$UISourceDir = Join-Path $UIProjectDir "bin" $Configuration $Framework
Copy-Item -Path "$UISourceDir\*" -Destination $RootOutputDir -Recurse -Force

# Get all plugin directories
$PluginDirs = Get-ChildItem -Path $PluginsBaseDir -Directory

# Copy each plugin
foreach ($PluginDir in $PluginDirs) {
    $PluginName = $PluginDir.Name
    Write-Host "Copying plugin: $PluginName..."
    $PluginSourceDir = Join-Path $PluginDir "bin" $Configuration $Framework
    
    if (Test-Path $PluginSourceDir) {
        $PluginDestDir = Join-Path $PluginsDestDir $PluginName
        New-Item -ItemType Directory -Path $PluginDestDir -Force | Out-Null
        Copy-Item -Path "$PluginSourceDir\*" -Destination $PluginDestDir -Recurse -Force
    } else {
        Write-Warning "Plugin source for $PluginName not found."
    }
}

Write-Host ""
Write-Host "Build and assembly complete!"
Write-Host "The application is ready in: $RootOutputDir"