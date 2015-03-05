[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [String]$ClusterUrl,
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword,
    [Parameter(Mandatory = $true)]
    [string]$SpecFile,
    [Parameter(Mandatory = $true)]
    [string]$PackageFile
)

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

$scpnetSubmitterDir = Join-Path $scriptDir "..\..\tools\SCPNetTopologySubmitter"
$scpnetSubmitterBinDir = Join-Path $scpnetSubmitterDir "bin\Debug"
$scpNetSubmitter = Join-Path $scpnetSubmitterBinDir "SCPNetTopologySubmitter.exe"

if(-not (Test-Path $scpNetSubmitter))
{
    & "$scriptDir\..\build\buildCSharp.bat" $scpnetSubmitterDir
    if($LASTEXITCODE -ne 0)
    {
        throw "ERROR: $scpNetSubmitter not found. Please make sure you have built the SCPNetTopologySubmitter project before running this script."
    }
}

& "$scpNetSubmitter" $ClusterUrl $ClusterUsername $ClusterPassword $SpecFile $PackageFile

if($LASTEXITCODE -ne 0)
{
    throw "ERROR: Failed to submit topology. Please check error logs for more information."
}