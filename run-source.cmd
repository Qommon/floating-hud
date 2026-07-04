@echo off
setlocal

cd /d "%~dp0"
dotnet run --project "src\floating-hud.csproj" -c Release -- %*

if errorlevel 1 (
    echo.
    echo Floating HUD failed to start.
    pause
)
