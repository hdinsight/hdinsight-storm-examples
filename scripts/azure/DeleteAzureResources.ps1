[CmdletBinding(PositionalBinding=$True)]
Param(
    [parameter(Mandatory=$true)]
    [string]$ExampleDir
    )

###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit -9999
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

###########################################################
# Main Script
###########################################################

# Make sure you run this in Microsoft Azure Powershell prompt
if(-not (& "$scriptDir\CheckAzurePowershell.ps1"))
{
    Write-ErrorLog "Check Azure Powershell Failed! You need to run this script from Azure Powershell." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Check Azure Powershell Failed! You need to run this script from Azure Powershell."
}

$startTime = Get-Date

Write-SpecialLog "Deleting Azure resources for example: $ExampleDir" (Get-ScriptName) (Get-ScriptLineNumber)

$configFile = Join-Path $ExampleDir "run\configurations.properties"
$config = & "$scriptDir\..\config\ReadConfig.ps1" $configFile

$config.Keys | sort | % { if(-not ($_.Contains("PASSWORD") -or $_.Contains("KEY"))) { Write-SpecialLog ("Key = " + $_ + ", Value = " + $config[$_]) (Get-ScriptName) (Get-ScriptLineNumber) } }

$subName = $config["AZURE_SUBSCRIPTION_NAME"]
Switch-AzureMode -Name AzureServiceManagement
Write-SpecialLog "Using subscription '$subName'" (Get-ScriptName) (Get-ScriptLineNumber)
Select-AzureSubscription -SubscriptionName $subName

#Changing Error Action to Continue here onwards to have maximum resource deletion
$ErrorActionPreference = "Continue"

$eventhub = $false
if($config["EVENTHUBS"].Equals("true", [System.StringComparison]::OrdinalIgnoreCase))
{
    $eventhub = $true
}

$docdb = $false
if($config["DOCUMENTDB"].Equals("true", [System.StringComparison]::OrdinalIgnoreCase))
{
    $docdb = $true
}

$sqlAzure = $false
if($config["SQLAZURE"].Equals("true", [System.StringComparison]::OrdinalIgnoreCase))
{
    $sqlAzure = $true
}

$hbase = $false
if($config["HBASE"].Equals("true", [System.StringComparison]::OrdinalIgnoreCase))
{
    $hbase = $true
}

$kafka = $false
if($config["KAFKA"].Equals("true", [System.StringComparison]::OrdinalIgnoreCase))
{
    $kafka = $true
}

$success = $true

Write-InfoLog "Deleting HDInsight Storm Cluster" (Get-ScriptName) (Get-ScriptLineNumber)
& "$scriptDir\HDInsight\DeleteCluster.ps1" $config["STORM_CLUSTER_NAME"]
$success = $success -and $?

if($hbase)
{
    Write-InfoLog "Deleting HDInsight HBase Cluster" (Get-ScriptName) (Get-ScriptLineNumber)
    & "$scriptDir\HDInsight\DeleteCluster.ps1" $config["HBASE_CLUSTER_NAME"]
    $success = $success -and $?
}

if($kafka)
{
    Write-InfoLog "Deleting HDInsight Kafka on Storm Cluster" (Get-ScriptName) (Get-ScriptLineNumber)
    & "$scriptDir\HDInsight\DeleteCluster.ps1" $config["KAFKA_CLUSTER_NAME"]
    $success = $success -and $?
}

if($eventhub)
{
    Write-InfoLog "Deleting EventHubs" (Get-ScriptName) (Get-ScriptLineNumber)
    & "$scriptDir\EventHubs\DeleteEventHubs.ps1" $config["EVENTHUBS_NAMESPACE"] $config["EVENTHUBS_ENTITY_PATH"]
    $success = $success -and $?
}

if($docdb)
{
    Write-InfoLog "Deleting DocumentDB" (Get-ScriptName) (Get-ScriptLineNumber)
    & "$scriptDir\DocumentDB\DeleteDocumentDB.ps1" $config["DOCUMENTDB_ACCOUNT"]
    $success = $success -and $?
}

if($sqlAzure)
{
    Write-InfoLog "Deleting SQL Azure" (Get-ScriptName) (Get-ScriptLineNumber)
    & "$scriptDir\SqlAzure\DeleteSqlAzure.ps1" $config["SQLAZURE_SERVER_NAME"]
    $success = $success -and $?
}

Write-InfoLog "Deleting Storage Account" (Get-ScriptName) (Get-ScriptLineNumber)
& "$scriptDir\Storage\DeleteStorageAccount.ps1" $config["WASB_ACCOUNT_NAME"]
$success = $success -and $?

Write-SpecialLog "Deleting Azure Virtual Network" (Get-ScriptName) (Get-ScriptLineNumber)
$VNetConfig = & "$scriptDir\VirtualNetwork\DeleteVNet.ps1"
$success = $success -and $?

if($success)
{
    Write-SpecialLog "Deleting configuration.properties file" (Get-ScriptName) (Get-ScriptLineNumber)
    Remove-Item $configFile
    $totalSeconds = ((Get-Date) - $startTime).TotalSeconds
    Write-SpecialLog "Deleted Azure resources, completed in $totalSeconds seconds" (Get-ScriptName) (Get-ScriptLineNumber)
}
else
{
    Write-ErrorLog "One or more errors occurred during Azure resource deletion. Please check logs for error information." (Get-ScriptName) (Get-ScriptLineNumber)
    Write-ErrorLog "Please retry and delete your configuration file manually from: $configFile" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "One or more errors occurred during Azure resource deletion. Please check logs for error information."
}