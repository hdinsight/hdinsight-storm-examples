$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

$nugetExe = Join-Path $scriptDir "..\..\..\tools\nuget\nuget.exe"
$install = & "$nugetExe" install WindowsAzure.ServiceBus -version 2.6.1 -OutputDirectory "$scriptDir\..\..\..\packages"

$sbNuget = (gci "$scriptDir\..\..\..\packages\WindowsAzure.ServiceBus.*")[0].FullName

$sbDll = Join-Path $sbNuget "lib\net40-full\Microsoft.ServiceBus.dll"

if(-not (Test-Path $sbDll))
{
    throw "ERROR: $sbDll not found. Please make sure you have WindowsAzure.ServiceBus nuget package available."
}

return $sbDll