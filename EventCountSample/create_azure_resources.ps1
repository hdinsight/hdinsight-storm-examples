# Make sure you run this in Microsoft Azure Powershell prompt
$AzurePowershellPath="..\azurepowershell"
if(-not (& "$AzurePowershellPath\check_azure_powershell.ps1"))
{
    throw "Not running in Azure Powershell"
}

# Make sure you delete azure resources first
if(Test-Path ".\config\configurations.properties")
{
    throw "Please call delete_azure_resources.ps1 before creating new resources"
}

$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$result = Add-AzureAccount
if($result -eq $null)
{
    throw "Add Azure Account failure"
}
Switch-AzureMode -Name AzureServiceManagement

$subNames = Get-AzureSubscription | Foreach {$_.SubscriptionName}
Write-Host "Available Subscription Names: "
Write-Host $subNames

$subName = Read-Host "Enter subscription name"
Write-Host "Using subscription '$subName'"
#Select-AzureSubscription -SubscriptionName "<yoursubscription>"
Select-AzureSubscription -SubscriptionName $subName

$startTime = Get-Date

Write-Host "Step 0: Generating Random Configurations"
pushd config
.\generate_random_config.ps1
popd
$config = .\config\ReadConfig.ps1 ".\config\configurations.properties"

Write-Host "Step 1: Creating Storage and update storage key in configurations.properties"
$storagePassword = & "$AzurePowershellPath\Storage\CreateStorageAccount.ps1" $config["WASB_ACCOUNT_NAME"]
if($storagePassword -eq $null)
{
    throw "Cannot get storage key"
}
.\config\ReplaceStringInFile.ps1 ".\config\configurations.properties" ".\config\configurations.properties" @{WASB_ACCOUNT_KEY=$storagePassword}

Write-Host "Creating EventHubs, SQL Azure and HDInsight Storm in parallel..."

Write-Host "Step 2: Creating EventHubs of 32 partitions"
$scriptCreateEH = {
    param($config,$AzurePowershellPath,$workingDir,$subName)
    Set-Location $workingDir
    Select-AzureSubscription -SubscriptionName $subName
    & "$AzurePowershellPath\EventHubs\CreateEventHubs.ps1" $config["EVENTHUBS_NAMESPACE"] $config["EVENTHUBS_ENTITY_PATH"] $config["EVENTHUBS_USERNAME"] $config["EVENTHUBS_PASSWORD"] "West Europe" 32
}
$ehJob = Start-Job $scriptCreateEH -ArgumentList $config,$AzurePowershellPath,$pwd,$subName

Write-Host "Step 3: Creating SQL Azure and update SQL server name in configurations.properties"
$scriptCreateDocDb = {
    param($config,$AzurePowershellPath,$workingDir,$subName)
    Set-Location $workingDir
    Select-AzureSubscription -SubscriptionName $subName
    $serverName = & "$AzurePowershellPath\SqlAzure\CreateSqlDatabase.ps1" $config["SQLAZURE_DB_NAME"] $config["SQLAZURE_USER"] $config["SQLAZURE_PASSWORD"]
    .\config\ReplaceStringInFile.ps1 ".\config\configurations.properties" ".\config\configurations.properties" @{SQLAZURE_SERVER_NAME=$serverName}
}
$dbJob = Start-Job $scriptCreateDocDb -ArgumentList $config,$AzurePowershellPath,$pwd,$subName

Write-Host "Step 4: Creating HDInsight Storm Cluster of 32 nodes"
$scriptCreateStorm = {
    param($config,$AzurePowershellPath,$workingDir,$subName)
    Set-Location $workingDir
    Select-AzureSubscription -SubscriptionName $subName
    & "$AzurePowershellPath\HDInsight\CreateStormCluster.ps1" $config["HDINSIGHT_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["HDINSIGHT_CLUSTER_USERNAME"] $config["HDINSIGHT_CLUSTER_PASSWORD"] 32
}
$stormJob = Start-Job $scriptCreateStorm -ArgumentList $config,$AzurePowershellPath,$pwd,$subName

While (Get-Job -State "Running")
{
    Start-Sleep 10
    Get-Job -Id $ehJob.Id | Receive-Job
    Get-Job -Id $dbJob.Id | Receive-Job
    Get-Job -Id $stormJob.Id | Receive-Job
}

Remove-Job $ehJob
Remove-Job $dbJob
Remove-Job $stormJob

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-Host "Successfully created Azure resources, completed in $totalSeconds seconds."