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
# EventGenTopology
###########################################################

Write-SpecialLog ("For benchmark test, go to Azure portal to set Throughput Unit to 20 for Service Bus " + $config["EVENTHUBS_NAMESPACE"]) (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog "Press any key to continue..." (Get-ScriptName) (Get-ScriptLineNumber)
cmd /c pause | out-null

$localJarPath = "$scriptDir\EventGenTopology\target\eventgen-1.0-jar-with-dependencies.jar"
$blobPath = "Storm/SubmittedJars/eventgen-1.0.jar"
$jarPath = "{0}{1}" -f "/",$blobPath
$className = "com.microsoft.hdinsight.storm.examples.EventGenTopology"

Write-SpecialLog "Submitting Storm topology to generate events" (Get-ScriptName) (Get-ScriptLineNumber)
$result = & "$scriptDir\..\scripts\azure\Storage\UploadFileToStorage.ps1" $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $localJarPath $blobPath
$result = & "$scriptDir\..\scripts\storm\SubmitStormTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] $jarPath $className "EventGenTopology"

#Sleep a while for topologies to get started
sleep -s 15

& "$scriptDir\..\scripts\storm\GetStormSummary.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]
& "$scriptDir\..\scripts\storm\LaunchStormUI.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]
Write-InfoLog "Press any key to continue..." (Get-ScriptName) (Get-ScriptLineNumber)
cmd /c pause | out-null

Write-InfoLog "We need to pre-populate the EventHubs partitions before starting the next topology: EventCountDbTopology." (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog "When you see the topology: EventGenTopology has finished sending events to EventHubs, you can kill the topology to free workers for next topology: EventCountDbTopology." (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog "It should take about 10 minutes for the topology: EventGenTopology to send all events." (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog "Kill the topology EventGenTopology using Storm UI before starting the next topology: EventCountDbTopology." (Get-ScriptName) (Get-ScriptLineNumber)
Write-SpecialLog "Press any key to submit event count topology: EventCountDbTopology ..." (Get-ScriptName) (Get-ScriptLineNumber)
cmd /c pause | out-null

###########################################################
# EventCountTopology
###########################################################

$localJarPath = "$scriptDir\EventCountTopology\target\eventcountdb-1.0-jar-with-dependencies.jar"
$blobPath = "Storm/SubmittedJars/eventcountdb-1.0.jar"
$jarPath = "{0}{1}" -f "/",$blobPath
$className = "com.microsoft.hdinsight.storm.examples.EventCountDbTopology"

Write-SpecialLog "Starting Storm topology to count events" (Get-ScriptName) (Get-ScriptLineNumber)
$result = & "$scriptDir\..\scripts\azure\Storage\UploadFileToStorage.ps1" $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $localJarPath $blobPath
$result = & "$scriptDir\..\scripts\storm\SubmitStormTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] $jarPath $className "EventCountDbTopology"

#Sleep a while for topologies to get started
sleep -s 15

& "$scriptDir\..\scripts\storm\GetStormSummary.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]

#create and open result.html to view the count
$resultTemplateFile = "$scriptDir\result.html.template"
$resultFile = "$scriptDir\run\result.html"
& "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" $resultTemplateFile $resultFile $config
Invoke-Item $resultFile