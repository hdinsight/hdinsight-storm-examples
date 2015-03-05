@echo off

:BUILD
if [%1] NEQ [] (pushd %1)
echo.
PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0buildJava.ps1"
IF %ERRORLEVEL% NEQ 0 (
    echo buildJava.ps1 returned non-zero exit code: %ERRORLEVEL%. Please check if build completed successfully before you can launch.
    goto ERROR
)

goto DONE
goto :eof

:ERROR
if [%1] NEQ [] (
popd
)
exit /b -1

:DONE
if [%1] NEQ [] (
popd
)
exit /b %ERRORLEVEL%