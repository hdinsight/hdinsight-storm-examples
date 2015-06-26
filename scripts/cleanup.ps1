[CmdletBinding(PositionalBinding=$True)]
Param(
    [parameter(Mandatory=$true)]
    [string]$ExampleDir
    )
    
###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit -9999
}
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

Remove-Item "$ExampleDir\*.log" -Force

if(Test-Path "$scriptDir\..\packages")
{
    Remove-Item "$scriptDir\..\packages" -Force -Recurse -ErrorAction SilentlyContinue
}

Remove-Module "Logging-HDInsightExamples" -Force -ErrorAction SilentlyContinue