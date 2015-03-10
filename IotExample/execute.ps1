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

$configFile = Join-Path $scriptDir "run\configurations.properties"
$config = & "$scriptDir\..\scripts\config\ReadConfig.ps1" $configFile
Select-AzureSubscription -SubscriptionName $config["AZURE_SUBSCRIPTION_NAME"]

###########################################################
# Event Generation
###########################################################

$failure = $false
$javaExe = "java.exe"

Write-InfoLog "Starting DocDbGen process" (Get-ScriptName) (Get-ScriptLineNumber)
$javaArgs = "-cp ""$scriptDir\docdbgen\target\docdbgen-1.0-jar-with-dependencies.jar"" com.microsoft.hdinsight.storm.examples.DocDbGen ""$scriptDir\run\docdb.config"" ""$scriptDir\vehiclevin.txt"""
$javaProcess = Start-Process -FilePath $javaExe -ArgumentList $javaArgs
if($javaProcess -ne $null)
{
    $javaProcess | Wait-Process
}
if((-not $?) -or ($LASTEXITCODE -ne 0))
{
    Write-ErrorLog "DocDbGen Process failed ($?) with non-zero exit code: $LASTEXITCODE" (Get-ScriptName) (Get-ScriptLineNumber)
    $failure = $true
}

Write-InfoLog "Starting EventGen process" (Get-ScriptName) (Get-ScriptLineNumber)
$javaArgs = "-jar ""$scriptDir\eventgen\target\eventgen-1.0-jar-with-dependencies.jar"" ""$scriptDir\run\eventhubs.config"" ""$scriptDir\vehiclevin.txt"" 200"
$javaProcess = Start-Process -FilePath $javaExe -ArgumentList $javaArgs
if($javaProcess -ne $null)
{
    $javaProcess | Wait-Process
}
if((-not $?) -or ($LASTEXITCODE -ne 0))
{
    Write-ErrorLog "EvenGen Process failed ($?) with non-zero exit code: $LASTEXITCODE" (Get-ScriptName) (Get-ScriptLineNumber)
    $failure = $true
}

if($failure)
{
    Write-ErrorLog "One or more event generation processes failed." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "One or more event generation processes failed."
}

###########################################################
# IoT Topology
###########################################################

$localJarPath = "$scriptDir\iot\target\iot-1.0.jar"
$blobPath = "Storm/SubmittedJars/iot-1.0.jar"
$jarPath = "{0}{1}" -f "/",$blobPath
$className = "com.microsoft.hdinsight.storm.examples.IotTopology"

Write-SpecialLog "Starting Storm topology for IoT" (Get-ScriptName) (Get-ScriptLineNumber)
$result = & "$scriptDir\..\scripts\azure\Storage\UploadFileToStorage.ps1" $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $localJarPath $blobPath
$result = & "$scriptDir\..\scripts\storm\SubmitStormTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] $jarPath $className "IotTopology"

#Sleep a while for topologies to get started
sleep -s 15

& "$scriptDir\..\scripts\storm\GetStormSummary.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]
& "$scriptDir\..\scripts\storm\LaunchStormUI.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]