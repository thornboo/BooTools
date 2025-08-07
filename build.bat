@echo off
echo Building Boo Tools...

set UI_OUTPUT_DIR=src\BooTools.UI\bin\Release\net8.0-windows

echo Building solution...
dotnet build BooTools.sln -c Release

echo.
echo Assembling application components...

REM Create the target Plugins directory in the main UI output folder
set PLUGINS_DEST_DIR=%UI_OUTPUT_DIR%\Plugins
echo Creating plugin directory at: %PLUGINS_DEST_DIR%
if not exist "%PLUGINS_DEST_DIR%" mkdir "%PLUGINS_DEST_DIR%"

REM --- Copy WallpaperSwitcher ---
set PLUGIN_NAME=WallpaperSwitcher
set PLUGIN_SOURCE_DIR=src\BooTools.Plugins\%PLUGIN_NAME%\bin\Release\net8.0-windows
set PLUGIN_TARGET_DIR=%PLUGINS_DEST_DIR%\%PLUGIN_NAME%
if exist "%PLUGIN_SOURCE_DIR%" (
    echo Copying %PLUGIN_NAME% from %PLUGIN_SOURCE_DIR%
    xcopy "%PLUGIN_SOURCE_DIR%" "%PLUGIN_TARGET_DIR%\" /s /e /i /y /q
)

REM --- Copy EnvironmentVariableEditor ---
set PLUGIN_NAME=EnvironmentVariableEditor
set PLUGIN_SOURCE_DIR=src\BooTools.Plugins\%PLUGIN_NAME%\bin\Release\net8.0-windows
set PLUGIN_TARGET_DIR=%PLUGINS_DEST_DIR%\%PLUGIN_NAME%
if exist "%PLUGIN_SOURCE_DIR%" (
    echo Copying %PLUGIN_NAME% from %PLUGIN_SOURCE_DIR%
    xcopy "%PLUGIN_SOURCE_DIR%" "%PLUGIN_TARGET_DIR%\" /s /e /i /y /q
)

echo.
echo Build and assembly complete!
echo Executable location: %UI_OUTPUT_DIR%\BooTools.UI.exe
pause