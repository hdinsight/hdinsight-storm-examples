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
Remove-Item "$scriptDir\EventGenTopology\target" -Force -Recurse
Remove-Item "$scriptDir\EventGenTopology\src\main\resources" -Force -Recurse

Remove-Item "$scriptDir\EventCountTopology\target" -Force -Recurse
Remove-Item "$scriptDir\EventCountTopology\src\main\resources"  -Force -Recurse

Remove-Item "$scriptDir\EventCountHybridTopology\bin" -Force -Recurse
Remove-Item "$scriptDir\EventCountHybridTopology\obj" -Force -Recurse
Remove-Item "$scriptDir\EventCountHybridTopology\packages" -Force -Recurse

Remove-Item "$scriptDir\EventCountHybridTopology\*.log" -Force
Remove-Item "$scriptDir\EventCountHybridTopology\*.spec" -Force
Remove-Item "$scriptDir\EventCountHybridTopology\*.zip" -Force
Remove-Item "$scriptDir\EventCountHybridTopology\*.suo" -Force
Remove-Item "$scriptDir\EventCountHybridTopology\*.user" -Force

cmd /c "git checkout -- ""$scriptDir\EventCountHybridTopology\SCPHost.exe.config"" 2>&1" | Out-Null
if($LASTEXITCODE -ne 0)
{
    Write-WarnLog "Failed to revert '$scriptDir\EventCountHybridTopology\SCPHost.exe.config'." (Get-ScriptName) (Get-ScriptLineNumber)
    Write-WarnLog "Please revert the file manually from Git Shell using 'git checkout -- ""$scriptDir\EventCountHybridTopology\SCPHost.exe.config""" (Get-ScriptName) (Get-ScriptLineNumber)
}

#Run Azure Cleanup
& "$scriptDir\..\scripts\cleanup.ps1" "$scriptDir"