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

#Delete Project Resources
Remove-Item "$scriptDir\docdbgen\target" -Force -Recurse

Remove-Item "$scriptDir\eventgen\target" -Force -Recurse

Remove-Item "$scriptDir\iot\target" -Force -Recurse
Remove-Item "$scriptDir\iot\src\main\resources" -Force -Recurse

#Run Azure Cleanup
& "$scriptDir\..\scripts\cleanup.ps1" "$scriptDir"
