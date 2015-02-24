# Make sure you run this in Microsoft Azure Powershell prompt
$AzurePowershellPath="..\azurepowershell"
if(-not (& "$AzurePowershellPath\check_azure_powershell.ps1"))
{
    throw "Not running in Azure Powershell"
}

$VerbosePreference = "SilentlyContinue"
#do not stop on error, clean as much resource as possible
$ErrorActionPreference = "Continue"

#We can assume Azure subscription has been selected
#Select-AzureSubscription -SubscriptionName "<yoursubscription>"

$startTime = Get-Date

$config = .\config\ReadConfig.ps1 ".\config\configurations.properties"

Write-Host "Deleting HDInsight Storm Cluster"
& "$AzurePowershellPath\HDInsight\DeleteStormCluster.ps1" $config["HDINSIGHT_CLUSTER_NAME"]

Write-Host "Deleting EventHubs"
& "$AzurePowershellPath\EventHubs\DeleteEventHubs.ps1" $config["EVENTHUBS_NAMESPACE"] $config["EVENTHUBS_ENTITY_PATH"]

Write-Host "Deleting SQL Azure"
& "$AzurePowershellPath\SqlAzure\DeleteSqlDatabase.ps1" $config["SQLAZURE_SERVER_NAME"] $config["SQLAZURE_DB_NAME"] $config["SQLAZURE_USER"] $config["SQLAZURE_PASSWORD"]

Write-Host "Deleting Storage Account"
& "$AzurePowershellPath\Storage\DeleteStorageAccount.ps1" $config["WASB_ACCOUNT_NAME"]

Write-Host "Deleting configuration.properties file"
Remove-Item config\configurations.properties

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-Host "Successfully cleaned Azure resources, completed in $totalSeconds seconds"