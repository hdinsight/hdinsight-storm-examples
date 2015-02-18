@echo off
PowerShell.exe -ExecutionPolicy Bypass -File %~dp0cleanup.ps1
exit /b %ERRORLEVEL%