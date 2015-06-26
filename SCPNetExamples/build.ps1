###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\scripts\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

#Download SCP.Net
$scpTools = & "$scriptDir\..\scripts\scpnet\GetScpTools.ps1"

#Copy into a sdk directory
Write-SpecialLog "Copying Microsoft.SCP.Net.SDK into $scriptDir\sdk" (Get-ScriptName) (Get-ScriptLineNumber)

$dummy = New-Item -ItemType Directory -Force -Path "$scriptDir\sdk"
Copy-Item -Force "$scpTools\..\lib\*.*" "$scriptDir\sdk"
Copy-Item -Force "$scpTools\..\sdk\*.*" "$scriptDir\sdk"

& "$scriptDir\..\scripts\build\buildJava.bat" "$scriptDir"

if($LASTEXITCODE -eq 0)
{
    Write-SpecialLog "Build Complete for '$scriptDir!'" (Get-ScriptName) (Get-ScriptLineNumber)
}
else
{
    Write-ErrorLog "Build returned non-zero exit code: $LASTEXITCODE. Please check if the project built successfully before you can launch examples." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Build returned non-zero exit code: $LASTEXITCODE. Please check if the project built successfully before you can launch examples."
}
& "$scriptDir\..\scripts\build\buildCSharp.bat" "$scriptDir"

if($LASTEXITCODE -eq 0)
{
    Write-SpecialLog "Build Complete for '$scriptDir!'" (Get-ScriptName) (Get-ScriptLineNumber)
}
else
{
    Write-ErrorLog "Build returned non-zero exit code: $LASTEXITCODE. Please check if the project built successfully before you can launch examples." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Build returned non-zero exit code: $LASTEXITCODE. Please check if the project built successfully before you can launch examples."
}