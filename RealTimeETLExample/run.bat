@echo off
pushd "%~dp0"
PowerShell.exe -ExecutionPolicy Bypass -Command "& { $ErrorActionPreference = 'Stop'; & '%~dp0run.ps1'; EXIT $LASTEXITCODE }"
if %ERRORLEVEL% NEQ 0 (
    echo Run failed! Please check logs for error information.
)
popd
exit /b %ERRORLEVEL%