@echo off

echo HDInsight Storm Examples Build
echo ==============================

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
echo.
PowerShell.exe -ExecutionPolicy Bypass -File %~dp0build.ps1
IF %ERRORLEVEL% EQU 0 (
PowerShell.exe -ExecutionPolicy Bypass -File %~dp0run.ps1
) ELSE (
echo build.ps1 returned non-zero exit code: %ERRORLEVEL%. Please check if build completed successfully before you can launch examples through run.ps1
)
goto DONE
goto :eof

:ERROR
exit /b -1

:DONE
exit /b %ERRORLEVEL%