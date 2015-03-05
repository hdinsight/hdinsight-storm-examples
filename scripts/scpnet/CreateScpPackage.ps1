[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [String]$TopologyAssemblyDir,
    [Parameter(Mandatory = $true)]
    [string]$PackageFile,
    [string]$JavaDependencies
)
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

$scpC = & "$scriptDir\GetScpTools.ps1"

Write-InfoLog "Creating Scp Package for $TopologyAssemblyDir at $PackageFile" (Get-ScriptName) (Get-ScriptLineNumber)

& "$scpC" package -cSharpTarget $TopologyAssemblyDir -packageFile $PackageFile -javaDependencies $JavaDependencies

if($LASTEXITCODE -ne 0)
{
    throw "ERROR: Failed to generate package file. Please check error logs for more information."
}