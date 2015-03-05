$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

$nugetExe = Join-Path $scriptDir "..\..\tools\nuget\nuget.exe"
$install = & "$nugetExe" install Microsoft.SCP.Net.SDK -version 0.9.4.203 -OutputDirectory "$scriptDir\..\..\packages"

$scpNuget = (gci "$scriptDir\..\..\packages\Microsoft.SCP.Net.SDK.*")[0].FullName
$scpTools = Join-Path $scpNuget "tools"

#Workaround to get ScpC.exe to work without invoking MSBuild
Copy-Item -Force ($scpNuget + "\lib\Microsoft.SCPNet.dll") $scpTools
Copy-Item -Force ($scpNuget + "\sdk\NewtonSoft.Json.dll") $scpTools

$scpC = Join-Path $scpTools "ScpC.exe"

if(-not (Test-Path $scpC))
{
    throw "ERROR: $scpC not found. Please make sure you have Microsoft.SCP.Net.SDK nuget package available."
}

return $scpC