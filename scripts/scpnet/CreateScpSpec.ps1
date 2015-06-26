[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [String]$TopologyAssembly,
    [Parameter(Mandatory = $true)]
    [string]$SpecFile,
    [string]$FullyQualifiedClassName
)

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

$scpTools = & "$scriptDir\GetScpTools.ps1"
$scpC = Join-Path $scpTools "ScpC.exe"

Write-InfoLog "Creating Scp spec for $TopologyAssembly at $SpecFile. ClassName (if any): $FullyQualifiedClassName" (Get-ScriptName) (Get-ScriptLineNumber)
& "$scpC" generate-spec -assembly $TopologyAssembly -spec $SpecFile -class $FullyQualifiedClassName

if(($LASTEXITCODE -ne 0) -or (-not (Test-Path $SpecFile)))
{
    Write-ErrorLog "ERROR: Failed to generate spec file. Please check error logs for more information." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "ERROR: Failed to generate spec file. Please check error logs for more information."
}
else
{
    Write-SpecialLog "Created Spec file at $SpecFile for $TopologyAssembly. ClassName (if any): $FullyQualifiedClassName" (Get-ScriptName) (Get-ScriptLineNumber)
}