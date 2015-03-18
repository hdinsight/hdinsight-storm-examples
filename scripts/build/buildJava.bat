@echo off

:BUILD
if [%1] NEQ [] (pushd %1)
echo.
PowerShell.exe -ExecutionPolicy Bypass -Command "& { $ErrorActionPreference = 'Stop'; & '%~dp0buildJava.ps1'; EXIT $LASTEXITCODE }"
IF %ERRORLEVEL% NEQ 0 (
    echo buildJava.ps1 returned non-zero exit code: %ERRORLEVEL%. Please ensure build completes successfully before you can run the examples.
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