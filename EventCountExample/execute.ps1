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

Write-SpecialLog ("IMPORTANT: For benchmark test, go to Azure portal to set Throughput Unit to MAX(20) for Service Bus " + $config["EVENTHUBS_NAMESPACE"]) (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog "Press any key to continue ..." (Get-ScriptName) (Get-ScriptLineNumber)
cmd /c pause | out-null

$localJarPath = "$scriptDir\EventGenTopology\target\eventgen-1.0-jar-with-dependencies.jar"
$blobPath = "Storm/SubmittedJars/eventgen-1.0.jar"
$jarPath = "{0}{1}" -f "/",$blobPath
$className = "com.microsoft.hdinsight.storm.examples.EventGenTopology"

Write-SpecialLog "Submitting Storm topology to generate events" (Get-ScriptName) (Get-ScriptLineNumber)
$result = & "$scriptDir\..\scripts\azure\Storage\UploadFileToStorage.ps1" $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $localJarPath $blobPath
$result = & "$scriptDir\..\scripts\storm\SubmitStormTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] $jarPath $className "EventGenTopology"

Write-InfoLog "Waiting for a short while for topologies to get started ..." (Get-ScriptName) (Get-ScriptLineNumber)
sleep -s 15

& "$scriptDir\..\scripts\storm\GetStormSummary.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]
& "$scriptDir\..\scripts\storm\LaunchStormUI.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]

Write-InfoLog "We need to pre-populate the EventHubs partitions before starting the next topology: EventCountDbTopology." (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog "When you see the topology: EventGenTopology has finished sending events to EventHubs, you can kill the topology to free workers for next topology: EventCountDbTopology." (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog "It should take about 10 minutes for the topology: EventGenTopology to send all events." (Get-ScriptName) (Get-ScriptLineNumber)
Write-SpecialLog "IMPORTANT: Please kill the topology EventGenTopology using Storm UI before starting the next topology: EventCountDbTopology." (Get-ScriptName) (Get-ScriptLineNumber)
Write-SpecialLog "Press any key to submit event count topology: EventCountDbTopology ..." (Get-ScriptName) (Get-ScriptLineNumber)
cmd /c pause | out-null

###########################################################
# EventCountTopology
###########################################################

$localJarPath = "$scriptDir\EventCountTopology\target\eventcountdb-1.0-jar-with-dependencies.jar"
$blobPath = "Storm/SubmittedJars/eventcountdb-1.0.jar"
$jarPath = "{0}{1}" -f "/",$blobPath
$className = "com.microsoft.hdinsight.storm.examples.EventCountDbTopology"
$topologyName = "EventCountDbTopology" + [System.DateTime]::Now.ToString("yyyyMMddHHmmss")

Write-SpecialLog "Starting Storm topology to count events" (Get-ScriptName) (Get-ScriptLineNumber)
$result = & "$scriptDir\..\scripts\azure\Storage\UploadFileToStorage.ps1" $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $localJarPath $blobPath
$result = & "$scriptDir\..\scripts\storm\SubmitStormTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] $jarPath $className $topologyName

Write-InfoLog "Waiting for a short while for topologies to get started ..." (Get-ScriptName) (Get-ScriptLineNumber)
sleep -s 15

& "$scriptDir\..\scripts\storm\GetStormSummary.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]

#create and open result.html to view the count
$resultTemplateFile = "$scriptDir\result.html.template"
$resultFile = "$scriptDir\run\result.html"
& "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" $resultTemplateFile $resultFile $config
Invoke-Item $resultFile

###########################################################
# EventCountHybridTopology
###########################################################
Write-SpecialLog "Please enter 'y' if you wish to run the EventCountHybridTopology which uses SCP.Net to measure the event counts" (Get-ScriptName) (Get-ScriptLineNumber)
$runHybrid = Read-Host "Do you also want to run the EventCountHybridTopology to see event counts using SCP.Net for the same event hub"

if(($runHybrid -like "y") -or ($runHybrid -like "yes"))
{
    Write-SpecialLog "IMPORTANT: Please kill any existing topologies using Storm UI before starting the next topology: EventCountHybridTopology" (Get-ScriptName) (Get-ScriptLineNumber)
    Write-SpecialLog "Press any key to submit event count hybrid topology: EventCountHybridTopology ..." (Get-ScriptName) (Get-ScriptLineNumber)
    cmd /c pause | out-null

    Write-SpecialLog "Starting Storm hybrid topology to count events" (Get-ScriptName) (Get-ScriptLineNumber)    
    
    $topologyDir = Join-Path $scriptDir "EventCountHybridTopology"
    $topologyBinDir = Join-Path $topologyDir "bin\Debug"

    Remove-Item -Recurse "$topologyBinDir\ScpPackage" -ErrorAction SilentlyContinue
    
    & "$scriptDir\..\scripts\scpnet\CreateScpSpec.ps1" "$topologyBinDir\EventCountHybridTopology.dll" "$topologyDir\EventCountHybridTopology.spec" "EventCountHybridTopology.EventCountHybridTopology"
    & "$scriptDir\..\scripts\scpnet\CreateScpPackage.ps1" "$topologyBinDir" "$topologyDir\EventCountHybridTopology.zip" "$scriptDir\..\lib\eventhubs"
    & "$scriptDir\..\scripts\scpnet\SubmitSCPNetTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] "$topologyDir\EventCountHybridTopology.spec" "$topologyDir\EventCountHybridTopology.zip"
    
    Write-InfoLog "Waiting for a short while for topologies to get started ..." (Get-ScriptName) (Get-ScriptLineNumber)
    sleep -s 15

    & "$scriptDir\..\scripts\storm\GetStormSummary.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]

    #create and open result.html to view the count
    $resultTemplateFile = "$scriptDir\result_hybrid.html.template"
    $resultFile = "$scriptDir\run\result_hybrid.html"
    & "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" $resultTemplateFile $resultFile $config
    Invoke-Item $resultFile
}
