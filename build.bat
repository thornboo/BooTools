@echo off
setlocal

:: Configuration
set "CONFIG=Release"
set "FRAMEWORK=net8.0-windows"
set "ROOT_OUTPUT_DIR=.\bin"
set "UI_PROJECT_DIR=.\src\BooTools.UI"
set "PLUGINS_BASE_DIR=.\src\BooTools.Plugins"

:: Clean previous build
echo Cleaning up old build artifacts...
if exist "%ROOT_OUTPUT_DIR%" (
    rmdir /s /q "%ROOT_OUTPUT_DIR%"
)
echo.

:: Build solution
echo Building the entire solution...
dotnet build BooTools.sln -c %CONFIG%
if %errorlevel% neq 0 (
    echo Solution build failed!
    goto Fail
)
echo.

:: Assemble files
echo Assembling application components...

:: Create output directories
mkdir "%ROOT_OUTPUT_DIR%"
set "PLUGINS_DEST_DIR=%ROOT_OUTPUT_DIR%\Plugins"
mkdir "%PLUGINS_DEST_DIR%"

:: Copy main application
echo Copying main UI application...
xcopy "%UI_PROJECT_DIR%\bin\%CONFIG%\%FRAMEWORK%\*" "%ROOT_OUTPUT_DIR%\" /s /e /i /y /q
if %errorlevel% neq 0 (
    echo Failed to copy UI application!
    goto Fail
)

:: Copy WallpaperSwitcher Plugin
set "PLUGIN_NAME=WallpaperSwitcher"
echo Copying plugin: %PLUGIN_NAME%...
set "PLUGIN_SOURCE_DIR=%PLUGINS_BASE_DIR%\%PLUGIN_NAME%\bin\%CONFIG%\%FRAMEWORK%"
if exist "%PLUGIN_SOURCE_DIR%" (
    xcopy "%PLUGIN_SOURCE_DIR%\*" "%PLUGINS_DEST_DIR%\%PLUGIN_NAME%\" /s /e /i /y /q
) else (
    echo Warning: Plugin source for %PLUGIN_NAME% not found.
)

:: Copy EnvironmentVariableEditor Plugin
set "PLUGIN_NAME=EnvironmentVariableEditor"
echo Copying plugin: %PLUGIN_NAME%...
set "PLUGIN_SOURCE_DIR=%PLUGINS_BASE_DIR%\%PLUGIN_NAME%\bin\%CONFIG%\%FRAMEWORK%"
if exist "%PLUGIN_SOURCE_DIR%" (
    xcopy "%PLUGIN_SOURCE_DIR%\*" "%PLUGINS_DEST_DIR%\%PLUGIN_NAME%\" /s /e /i /y /q
) else (
    echo Warning: Plugin source for %PLUGIN_NAME% not found.
)

echo.
echo Build and assembly complete!
echo The application is ready in: "%ROOT_OUTPUT_DIR%"
goto Success

:Fail
echo.
echo Build script failed.
set "EXIT_CODE=1"
goto End

:Success
echo.
echo Build script finished successfully.
set "EXIT_CODE=0"

:End
endlocal & exit /b %EXIT_CODE%