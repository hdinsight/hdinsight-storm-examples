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

###########################################################
# 1 - Prepare
###########################################################
Write-SpecialLog "Phase 1 - Prepare - Start - Preparing project configuration for $ExampleDir." (Get-ScriptName) (Get-ScriptLineNumber)

if(-not (Test-Path "$ExampleDir\prepare.ps1"))
{
    Write-ErrorLog "$ExampleDir\prepare.ps1 not found." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "$ExampleDir\prepare.ps1 not found."
}

& "$ExampleDir\prepare.ps1"

if(-not $?)
{
    Write-ErrorLog "Preparation of configuration failed for $ExampleDir." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Preparation of configuration failed for $ExampleDir."
}

Write-SpecialLog "Phase 1 - Preparation - End - Preparation complete for $ExampleDir."

###########################################################
# 2 - Build
###########################################################
Write-SpecialLog "Phase 2 - Build - Start - Building $ExampleDir"

if(-not (Test-Path "$ExampleDir\build.ps1"))
{
    Write-ErrorLog "$ExampleDir\build.ps1 not found." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "$ExampleDir\build.ps1 not found."
}

& "$ExampleDir\build.ps1"

if(-not $?)
{
    Write-ErrorLog "Build failed! Please check if the project built successfully before you can launch examples." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Build failed! Please check if the project built successfully before you can launch examples."
}
Write-SpecialLog "Build complete for $ExampleDir" (Get-ScriptName) (Get-ScriptLineNumber)

###########################################################
# 3 - Execute
###########################################################
Write-SpecialLog "Phase 3 - Execute - Start - Executing the example in $ExampleDir"

if(-not (Test-Path "$ExampleDir\execute.ps1"))
{
    Write-ErrorLog "$ExampleDir\execute.ps1 not found." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "$ExampleDir\execute.ps1 not found."
}

& "$ExampleDir\execute.ps1"

if(-not $?)
{
    Write-ErrorLog "Execute resulted in an error. Please check the error logs for more information." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Execute resulted in an error. Please check the error logs for more information."
}

Write-SpecialLog "Phase 3 - Execute - End - Executed the example in $ExampleDir"

#Finish
Write-SpecialLog "Run Complete! Please use cleanup.bat for deleting all the created resources." (Get-ScriptName) (Get-ScriptLineNumber)