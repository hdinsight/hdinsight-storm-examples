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

#Try to delete as much as we can
$ErrorActionPreference = "SilentlyContinue"

Remove-Item "$scriptDir\EventHubAggregatorToHBaseTopology\bin" -Force -Recurse
Remove-Item "$scriptDir\EventHubAggregatorToHBaseTopology\obj" -Force -Recurse
Remove-Item "$scriptDir\EventHubAggregatorToHBaseTopology\packages" -Force -Recurse

Remove-Item "$scriptDir\EventHubAggregatorToHBaseTopology\*.log" -Force
Remove-Item "$scriptDir\EventHubAggregatorToHBaseTopology\*.spec" -Force
Remove-Item "$scriptDir\EventHubAggregatorToHBaseTopology\*.zip" -Force
Remove-Item "$scriptDir\EventHubAggregatorToHBaseTopology\*.suo" -Force
Remove-Item "$scriptDir\EventHubAggregatorToHBaseTopology\*.user" -Force

cmd /c "git checkout -- ""$scriptDir\EventHubAggregatorToHBaseTopology\SCPHost.exe.config"""

#Run Azure Cleanup
& "$scriptDir\..\scripts\cleanup.ps1" "$scriptDir"
