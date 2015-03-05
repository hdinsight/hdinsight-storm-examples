@echo off
pushd "%~dp0"
PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0cleanup.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo Cleanup failed! Please check logs for error information.
)
popd
exit /b %ERRORLEVEL%