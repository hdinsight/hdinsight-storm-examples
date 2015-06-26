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
$projects = gci -Directory

$projects | % {
	Write-InfoLog "Cleaning $_" (Get-ScriptName) (Get-ScriptLineNumber)

	Remove-Item "$scriptDir\$_\bin" -Force -Recurse
	Remove-Item "$scriptDir\$_\obj" -Force -Recurse
	Remove-Item "$scriptDir\$_\packages" -Force -Recurse

	Remove-Item "$scriptDir\$_\*.log" -Recurse -Force
	Remove-Item "$scriptDir\$_\*.out" -Recurse -Force
	Remove-Item "$scriptDir\$_\*.spec" -Force
	Remove-Item "$scriptDir\$_\*.zip" -Force
	Remove-Item "$scriptDir\$_\*.suo" -Force
	Remove-Item "$scriptDir\$_\*.user" -Force
	Remove-Item "$scriptDir\$_\SubmitConfig.xml" -Force
}

Remove-Item "$scriptDir\*.log" -Recurse -Force
Remove-Item "$scriptDir\*.out" -Recurse -Force
    
#Run Azure Cleanup
#& "$scriptDir\..\scripts\cleanup.ps1" "$scriptDir"