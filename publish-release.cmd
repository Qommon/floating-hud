@echo off
setlocal

cd /d "%~dp0"

set "RUNTIME=win-x64"
set "FRAMEWORK_SINGLE_FILE_OUTPUT=%CD%\artifacts\%RUNTIME%-framework-dependent-singlefile"
set "FRAMEWORK_FOLDER_OUTPUT=%CD%\artifacts\%RUNTIME%-framework-dependent"
set "SELF_CONTAINED_SINGLE_FILE_OUTPUT=%CD%\artifacts\%RUNTIME%-self-contained-singlefile"
set "SELF_CONTAINED_FOLDER_OUTPUT=%CD%\artifacts\%RUNTIME%-self-contained"

call :PublishRelease false true "%FRAMEWORK_SINGLE_FILE_OUTPUT%" "framework-dependent single-file" || exit /b 1
call :PublishRelease false false "%FRAMEWORK_FOLDER_OUTPUT%" "framework-dependent folder" || exit /b 1
call :PublishRelease true true "%SELF_CONTAINED_SINGLE_FILE_OUTPUT%" "self-contained single-file" || exit /b 1
call :PublishRelease true false "%SELF_CONTAINED_FOLDER_OUTPUT%" "self-contained folder" || exit /b 1

echo.
echo Floating HUD releases published to:
echo Framework-dependent:
echo %FRAMEWORK_SINGLE_FILE_OUTPUT%
echo %FRAMEWORK_FOLDER_OUTPUT%
echo.
echo Self-contained:
echo %SELF_CONTAINED_SINGLE_FILE_OUTPUT%
echo %SELF_CONTAINED_FOLDER_OUTPUT%

exit /b 0

:PublishRelease
set "SELF_CONTAINED=%~1"
set "PUBLISH_SINGLE_FILE=%~2"
set "OUTPUT=%~3"
set "LABEL=%~4"

dotnet publish "src\floating-hud.csproj" ^
    -c Release ^
    -r %RUNTIME% ^
    --self-contained %SELF_CONTAINED% ^
    -p:PublishSingleFile=%PUBLISH_SINGLE_FILE% ^
    -o "%OUTPUT%"

if errorlevel 1 (
    echo.
    echo Floating HUD %LABEL% release publish failed.
    pause
    exit /b 1
)

exit /b 0
