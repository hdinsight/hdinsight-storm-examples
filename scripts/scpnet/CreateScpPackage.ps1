[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [String]$TopologyAssemblyDir,
    [Parameter(Mandatory = $true)]
    [string]$PackageFile,
    [string]$JavaDependencies
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

Write-InfoLog "Creating Scp Package for $TopologyAssemblyDir at $PackageFile" (Get-ScriptName) (Get-ScriptLineNumber)

& "$scpC" package -cSharpTarget $TopologyAssemblyDir -packageFile $PackageFile -javaDependencies $JavaDependencies

if($LASTEXITCODE -ne 0)
{
    throw "ERROR: Failed to generate package file. Please check error logs for more information."
}