# Make sure you run this in Microsoft Azure Powershell prompt
$AzurePowershellPath="..\azurepowershell"
if(-not (& "$AzurePowershellPath\check_azure_powershell.ps1"))
{
    throw "Not running in Azure Powershell"
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
.\generate_random_config.ps1
$config = .\config\ReadConfig.ps1 ".\config\configurations.properties"

Write-Host "Step 1: Creating Storage and update storage key in configurations.properties"
$storagePassword = & "$AzurePowershellPath\Storage\CreateStorageAccount.ps1" $config["WASB_ACCOUNT_NAME"]
if($storagePassword -eq $null)
{
    throw "Cannot get storage key"
}
.\config\ReplaceStringInFile.ps1 ".\config\configurations.properties" ".\config\configurations.properties" @{WASB_ACCOUNT_KEY=$storagePassword}

Write-Host "Creating EventHubs, DocumentDb and HDInsight Storm in parallel..."
#for some reason we need a small delay here in order for HDInsight Storm to get the storage account
sleep -s 10

Write-Host "Step 2: Creating EventHubs"
$scriptCreateEH = {
    param($config,$AzurePowershellPath,$workingDir,$subName)
    Set-Location $workingDir
    Select-AzureSubscription -SubscriptionName $subName
    & "$AzurePowershellPath\EventHubs\CreateEventHubs.ps1" $config["EVENTHUBS_NAMESPACE"] $config["EVENTHUBS_ENTITY_PATH"] $config["EVENTHUBS_USERNAME"] $config["EVENTHUBS_PASSWORD"]
}
$ehJob = Start-Job $scriptCreateEH -ArgumentList $config,$AzurePowershellPath,$pwd,$subName

Write-Host "Step 3: Creating DocumentDB and update account key in configurations.properties"
$scriptCreateDocDb = {
    param($config,$AzurePowershellPath,$workingDir,$subName)
    Set-Location $workingDir
    Select-AzureSubscription -SubscriptionName $subName
    $docdbKey = & "$AzurePowershellPath\DocumentDB\CreateDocumentDB.ps1" $config["DOCUMENTDB_ACCOUNT"]
    if($docdbKey -eq $null)
    {
        throw "Cannot get DocumentDB account key"
    }
    .\config\ReplaceStringInFile.ps1 ".\config\configurations.properties" ".\config\configurations.properties" @{DOCDB_KEY=$docdbKey}
}
$dbJob = Start-Job $scriptCreateDocDb -ArgumentList $config,$AzurePowershellPath,$pwd,$subName

Write-Host "Step 4: Creating HDInsight Storm Cluster"
$scriptCreateStorm = {
    param($config,$AzurePowershellPath,$workingDir,$subName)
    Set-Location $workingDir
    Select-AzureSubscription -SubscriptionName $subName
    & "$AzurePowershellPath\HDInsight\CreateStormCluster.ps1" $config["HDINSIGHT_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["HDINSIGHT_CLUSTER_USERNAME"] $config["HDINSIGHT_CLUSTER_PASSWORD"]
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