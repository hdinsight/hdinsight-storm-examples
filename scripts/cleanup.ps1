[CmdletBinding(PositionalBinding=$True)]
Param(
    [parameter(Mandatory=$true)]
    [string]$ExampleDir
    )
    
###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\scripts\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit /b -9999
}

$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

gci -Directory (Join-Path $scriptDir "..\tools") | % {
Remove-Item (Join-Path $_.FullName "bin") -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item (Join-Path $_.FullName "obj") -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item (Join-Path $_.FullName "run") -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item (Join-Path $_.FullName "*.log") -Force -Recurse -ErrorAction SilentlyContinue
}

$ExampleDir = $ExampleDir.Replace("""","")
& "$scriptDir\azure\DeleteAzureResources.ps1" "$ExampleDir"

if(Test-Path "$ExampleDir\run")
{
    Remove-Item "$ExampleDir\run" -Force -Recurse
}

if(Test-Path "$scriptDir\..\packages")
{
    Remove-Item "$scriptDir\..\packages" -Force -Recurse -ErrorAction SilentlyContinue
}

Remove-Module "Logging-HDInsightExamples" -Force -ErrorAction SilentlyContinue