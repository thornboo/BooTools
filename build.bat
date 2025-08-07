@echo off
echo Building Boo Tools...

REM Clean previous builds
if exist "bin" rmdir /s /q "bin"
if exist "Plugins" rmdir /s /q "Plugins"

REM Build Core library
echo Building BooTools.Core...
dotnet build src\BooTools.Core\BooTools.Core.csproj -c Release -o bin\BooTools.Core

REM Build Main application
echo Building BooTools.UI...
dotnet build src\BooTools.UI\BooTools.UI.csproj -c Release -o bin\BooTools.UI

REM Build Plugins
echo Building WallpaperSwitcher plugin...
dotnet build src\BooTools.Plugins\WallpaperSwitcher\WallpaperSwitcher.csproj -c Release -o bin\BooTools.UI\Plugins\WallpaperSwitcher
echo Building EnvironmentVariableEditor plugin...
dotnet build src\BooTools.Plugins\EnvironmentVariableEditor\EnvironmentVariableEditor.csproj -c Release -o bin\BooTools.UI\Plugins\EnvironmentVariableEditor

REM Copy dependencies
echo Copying dependencies...
copy "bin\BooTools.Core\*.dll" "bin\BooTools.UI\"
echo Dependencies copied.

echo Build complete!
echo Executable location: bin\BooTools.UI\BooTools.UI.exe
pause