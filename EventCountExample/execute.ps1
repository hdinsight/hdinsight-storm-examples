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

function Run-IngestionScenario($config, $ingestionName)
{
    Write-InfoLog "Running scenario: $ingestionName" (Get-ScriptName) (Get-ScriptLineNumber)
    Submit-EventGenTopology $config $ingestionName
    Write-SpecialLog "Press any key to submit event count topology: EventCountDbTopology for $ingestionName" (Get-ScriptName) (Get-ScriptLineNumber)
    cmd /c pause | out-null
    Submit-EventCountTopology $config $ingestionName
    
    if($config["STORM_CLUSTER_OS_TYPE"] -like "Windows")
    {
      Write-SpecialLog "Please enter 'y' if you wish to run the EventCountHybridTopology which uses SCP.Net to measure the event counts" (Get-ScriptName) (Get-ScriptLineNumber)
      
      $runHybrid = Read-Host "Do you also want to run the EventCountHybridTopology to see event counts using SCP.Net for the same $ingestionName"

      if(($runHybrid -like "y") -or ($runHybrid -like "yes"))
      {
        Write-SpecialLog "IMPORTANT: Please kill any existing topologies using Storm UI before starting the next topology: EventCountHybridTopology for $ingestionName" (Get-ScriptName) (Get-ScriptLineNumber)
        Write-SpecialLog "Press any key to submit event count hybrid topology: EventCountHybridTopology for $ingestionName" (Get-ScriptName) (Get-ScriptLineNumber)
        cmd /c pause | out-null
        Submit-EventCountHybridTopology $config
      }
    }
}

function Submit-Topology($config, $localJarPath, $blobPath, $jarPath, $className, $classArgs)
{
    if($config["STORM_CLUSTER_OS_TYPE"] -like "Windows")
    {
        $result = & "$scriptDir\..\scripts\azure\Storage\UploadFileToStorageARM.ps1" $config["AZURE_RESOURCE_GROUP"] $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $localJarPath $blobPath
        $result = & "$scriptDir\..\scripts\storm\SubmitStormTopology.ps1" $config["STORM_CLUSTER_OS_TYPE"] $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] $jarPath $className $classArgs
    }
    else
    {
        $sshUrl = $config["STORM_CLUSTER_URL"].Replace("https://", "").Replace(".azurehdinsight.net", "-ssh.azurehdinsight.net")
        $sshUsername = "ssh" + $config["STORM_CLUSTER_USERNAME"]
        $result = & "$scriptDir\..\scripts\storm\SubmitStormTopology.ps1" $config["STORM_CLUSTER_OS_TYPE"] $sshUrl $sshUsername $config["STORM_CLUSTER_PASSWORD"] $localJarPath $className $classArgs
    }
    
    
    Write-InfoLog "Waiting for a short while for topologies to get started ..." (Get-ScriptName) (Get-ScriptLineNumber)
    sleep -s 15

    & "$scriptDir\..\scripts\storm\GetStormSummary.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]
    & "$scriptDir\..\scripts\storm\LaunchStormUI.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] $config["STORM_CLUSTER_OS_TYPE"]
}

function Submit-EventGenTopology($config, $ingestionName)
{
    Write-SpecialLog "Submitting Storm topology to generate events" (Get-ScriptName) (Get-ScriptLineNumber)

    if($ingestionName -like "EventHubs")
    {
        Write-SpecialLog ("IMPORTANT: For benchmark test, go to Azure portal to set Throughput Unit to MAX(20) for Service Bus " + $config["EVENTHUBS_NAMESPACE"]) (Get-ScriptName) (Get-ScriptLineNumber)
        Write-InfoLog "Press any key to continue ..." (Get-ScriptName) (Get-ScriptLineNumber)
        cmd /c pause | out-null
    }
    
    $localJarPath = "$scriptDir\EventGenTopology\target\eventgen-1.0.jar"
    $blobPath = "Storm/SubmittedJars/eventgen-1.0.jar"
    $jarPath = "{0}{1}" -f "/",$blobPath
    $className = "com.microsoft.hdinsight.storm.examples.EventGenTopology"
    $classArgs = '"EventGenTopologyFor{0}" "{0}"' -f $ingestionName

    Submit-Topology $config $localJarPath $blobPath $jarPath $className $classArgs
    
    Write-InfoLog "We need to pre-populate the $ingestionName partitions before starting the next topology: EventCountDbTopology for $ingestionName" (Get-ScriptName) (Get-ScriptLineNumber)
    Write-InfoLog "When you see the topology: EventGenTopologyFor$ingestionName has finished sending events to $ingestionName, you can kill the topology to free workers for next topology: EventCountDbTopology for $ingestionName." (Get-ScriptName) (Get-ScriptLineNumber)
    Write-InfoLog "It should take about 10 minutes for the topology: EventGenTopologyFor$ingestionName to send all events." (Get-ScriptName) (Get-ScriptLineNumber)
    Write-SpecialLog "IMPORTANT: Please kill the topology EventGenTopologyFor$ingestionName using Storm UI before starting the next topology: EventCountDbTopology for $ingestionName." (Get-ScriptName) (Get-ScriptLineNumber)
}

function Submit-EventCountTopology($config, $ingestionName)
{
    Write-SpecialLog "Starting Storm topology to count events" (Get-ScriptName) (Get-ScriptLineNumber)
    $localJarPath = "$scriptDir\EventCountTopology\target\eventcountdb-1.0.jar"
    $blobPath = "Storm/SubmittedJars/eventcountdb-1.0.jar"
    $jarPath = "{0}{1}" -f "/",$blobPath
    $className = "com.microsoft.hdinsight.storm.examples.EventCountDbTopology"
    $topologyName = "EventCountTopologyFor$ingestionName" + [System.DateTime]::Now.ToString("yyyyMMddHHmmss")
    $classArgs = '"{0}" "{1}"' -f $topologyName, $ingestionName
    
    Submit-Topology $config $localJarPath $blobPath $jarPath $className $classArgs

    $config["INGESTION_NAME"] = $ingestionName
    
    #create and open result.html to view the count
    $resultTemplateFile = "$scriptDir\result.html.template"
    $resultFile = "$scriptDir\run\$ingestionName-result.html"
    & "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" $resultTemplateFile $resultFile $config
    Invoke-Item $resultFile
}

function Submit-EventCountHybridTopology($config)
{
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

    $config["INGESTION_NAME"] = $ingestionName
    
    #create and open result.html to view the count
    $resultTemplateFile = "$scriptDir\result_hybrid.html.template"
    $resultFile = "$scriptDir\run\$ingestionName-result_hybrid.html"
    & "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" $resultTemplateFile $resultFile $config
    
    Invoke-Item $resultFile
 
    sleep -s 60
    
    <#
    Write-SpecialLog "Please enter 'y' if you wish to kill the EventCountHybridTopology." (Get-ScriptName) (Get-ScriptLineNumber)
    $killTopology = Read-Host "Kill the EventCountHybridTopology?"
    if(($killTopology -like "y") -or ($killTopology -like "yes"))
    {
        $TopologySpecContent = Get-Content "$topologyDir\EventCountHybridTopology.spec"
        $TopologyName = $TopologySpecContent[0].Split() | Select-Object -Last 1
        & "$scriptDir\..\scripts\scpnet\ManageSCPNetTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] $TopologyName "kill"
    }
    #>
}

# MAIN
$configFile = Join-Path $scriptDir "run\configurations.properties"
$config = & "$scriptDir\..\scripts\config\ReadConfig.ps1" $configFile

Run-IngestionScenario $config "Kafka"

Run-IngestionScenario $config "EventHubs"
