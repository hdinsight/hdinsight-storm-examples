@echo off

set VSTOOLS_PATH="%VS120COMNTOOLS%"
goto LOADVSTOOLS

set VSTOOLS_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio 12.0\Common7\Tools\"
goto LOADVSTOOLS

set VSTOOLS_PATH="%VS110COMNTOOLS%"
goto LOADVSTOOLS

set VSTOOLS_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio 11.0\Common7\Tools\"
goto LOADVSTOOLS

echo ERROR: Could not locate Visual Studio 2012 or Visual Studio 2013 build tools in your path. Please try running this build script from a Visual Studio Command Prompt.
goto ERROR

:LOADVSTOOLS
IF EXIST %VSTOOLS_PATH%vsvars32.bat (
echo Loading Visual Studio build tools from %VSTOOLS_PATH%
call %VSTOOLS_PATH%vsvars32.bat
goto BUILD
)
goto :eof

:BUILD
if [%1] NEQ [] (pushd %1)
echo.
PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0buildCSharp.ps1"
IF %ERRORLEVEL% NEQ 0 (
echo build.ps1 returned non-zero exit code: %ERRORLEVEL%. Please check if build completed successfully before you can launch examples through run.ps1
goto ERROR
)

goto DONE
goto :eof

:ERROR
popd
exit /b -1

:DONE
if [%1] NEQ [] (
popd
)
exit /b %ERRORLEVEL%