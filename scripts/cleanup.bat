@echo off

if [%1] == [] (
    echo Please specify the example directory which you would like to clean resources for. It should have the run\configuration.properties in it.
    exit /b 1
)

echo Calling cleanup for %1
PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0cleanup.ps1" %1
exit /b %ERRORLEVEL%