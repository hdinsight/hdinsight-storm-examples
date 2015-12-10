[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ClusterOsType,                         # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterUrl,                            # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,                       # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword,                       # required
    [Parameter(Mandatory = $true)]
    [String]$JarPath,                               # required    path of the jar in WASB to submit
    [Parameter(Mandatory = $true)]
    [String]$ClassName,                             # required
    [String]$AdditionalParams                       # optional    at least include the topology name
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

if($ClusterOsType -like "Windows")
{
    & "$scriptDir\SubmitStormTopologyWindows.ps1" $ClusterUrl $ClusterUsername $ClusterPassword $JarPath $ClassName $AdditionalParams
}
else
{
    & "$scriptDir\SubmitStormTopologyLinux.ps1" $ClusterUrl $ClusterUsername $ClusterPassword $JarPath $ClassName $AdditionalParams
}
