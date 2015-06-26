###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit -9999
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

$nugetExe = Join-Path $scriptDir "..\..\tools\nuget\nuget.exe"

$scpNetVersion = "0.9.4.346"
Write-InfoLog "Getting SCP.Net Tools using Microsoft.SCP.Net.SDK version: $scpNetVersion" (Get-ScriptName) (Get-ScriptLineNumber)

$install = & "$nugetExe" install Microsoft.SCP.Net.SDK -version $scpNetVersion -OutputDirectory "$scriptDir\..\..\packages"

$scpNuget = "$scriptDir\..\..\packages\Microsoft.SCP.Net.SDK.$scpNetVersion"
$scpTools = Join-Path $scpNuget "tools"

if(-not (Test-Path $scpTools))
{
    Write-ErrorLog "ERROR: $scpTools not found. Please make sure you have Microsoft.SCP.Net.SDK $scpNetVersion NuGet package available." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "ERROR: $scpTools not found. Please make sure you have Microsoft.SCP.Net.SDK $scpNetVersion NuGet package available."
}

Write-InfoLog "SCP.Net tools path: $scpTools" (Get-ScriptName) (Get-ScriptLineNumber)
return $scpTools