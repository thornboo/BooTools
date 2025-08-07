@echo off
echo 正在构建 Boo Tools...

REM 清理之前的构建
if exist "bin" rmdir /s /q "bin"
if exist "Plugins" rmdir /s /q "Plugins"

REM 构建核心库
echo 构建 BooTools.Core...
dotnet build src\BooTools.Core\BooTools.Core.csproj -c Release -o bin\BooTools.Core

REM 构建主程序
echo 构建 BooTools.UI...
dotnet build src\BooTools.UI\BooTools.UI.csproj -c Release -o bin\BooTools.UI

REM 构建插件
echo 构建 WallpaperSwitcher 插件...
dotnet build src\BooTools.Plugins\WallpaperSwitcher\WallpaperSwitcher.csproj -c Release

REM 复制依赖文件
echo 复制依赖文件...
copy "bin\BooTools.Core\*.dll" "bin\BooTools.UI\"
echo 依赖文件复制完成

echo 构建完成！
echo 可执行文件位置: bin\BooTools.UI\BooTools.UI.exe
pause 