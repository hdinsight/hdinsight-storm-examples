[CmdletBinding(PositionalBinding=$True)]
Param(
    [parameter(Mandatory=$true)]
    [string]$ExampleDir
    )

###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit /b -9999
}

$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

###########################################################
# Main Script
###########################################################

# Make sure you run this in Microsoft Azure Powershell prompt
if(-not (& "$scriptDir\CheckAzurePowershell.ps1"))
{
    throw "Check Azure Powershell Failed! You need to run this script from Azure Powershell."
}

###########################################################
# Get Run Configuration
###########################################################
$configFile = Join-Path $ExampleDir "run\configurations.properties"
$config = & "$scriptDir\..\config\ReadConfig.ps1" $configFile

###########################################################
# Add Azure Account
###########################################################
$account = Get-AzureAccount
if($account -eq $null)
{
    $account = Add-AzureAccount
    if($account -eq $null)
    {
        Write-ErrorLog "Failed to add Azure Account." (Get-ScriptName) (Get-ScriptLineNumber)
        throw "Failed to add Azure Account."
    }
}
Write-SpecialLog ("Using Azure Account: " + $account.Name) (Get-ScriptName) (Get-ScriptLineNumber)

Switch-AzureMode -Name AzureServiceManagement

$subNames = Get-AzureSubscription | % { "`r`n" + $_.SubscriptionName + " [" + $_.SubscriptionId + "]" }
Write-InfoLog ("Available Subscription Names (Name [Id]):`r`n" + $subNames) (Get-ScriptName) (Get-ScriptLineNumber)

$subName = Read-Host "Enter subscription name"
Write-SpecialLog "Using subscription '$subName'" (Get-ScriptName) (Get-ScriptLineNumber)
Select-AzureSubscription -SubscriptionName $subName

#Update the Azure Subscription Id in config
& "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{AZURE_SUBSCRIPTION_NAME=$subName}

$startTime = Get-Date

###########################################################
# Check Azure Resource Creation List
###########################################################

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

###########################################################
# Create Azure Resources
###########################################################

Write-SpecialLog "Step 1: Creating storage account and updating key in configurations.properties" (Get-ScriptName) (Get-ScriptLineNumber)
$storageKey = & "$scriptDir\Storage\CreateStorageAccount.ps1" $config["WASB_ACCOUNT_NAME"]
if([String]::IsNullOrWhiteSpace($storageKey))
{
    throw "Cannot get storage key"
}
else
{
    & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{WASB_ACCOUNT_KEY=$storageKey}
}
#add a small delay here in order for dependent resources to get the storage account
sleep -s 15

if($sqlAzure)
{
    #Let's attempt to create SQL Azure first than trying in parallel as it's critical to not lose information as server name is generated during that
    #This can lead to leaks of new SQL Azure Servers on re-runs if previous task failed to save the value
    Write-SpecialLog "Step 1.1: Creating SQL Azure Server and updating values in configurations.properties" (Get-ScriptName) (Get-ScriptLineNumber)
    $sqlServerName = & "$scriptDir\SqlAzure\CreateSqlAzure.ps1" $config["SQLAZURE_SERVER_NAME"] $config["SQLAZURE_DB_NAME"] $config["SQLAZURE_USER"] $config["SQLAZURE_PASSWORD"]
    if(-not [String]::IsNullOrWhiteSpace($sqlServerName))
    {
        & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{SQLAZURE_SERVER_NAME=$sqlServerName}
    }
}

Write-SpecialLog "Step 2: Creating remaining resources in parallel" (Get-ScriptName) (Get-ScriptLineNumber)
    
if($eventhub)
{
    Write-SpecialLog "Creating EventHubs" (Get-ScriptName) (Get-ScriptLineNumber)
        
    $scriptCreateEH = {
        param($subName,$scriptDir,$configFile,$config)
        Select-AzureSubscription -SubscriptionName $subName
        & "$scriptDir\..\init.ps1"
        $ehPassword = & "$scriptDir\EventHubs\CreateEventHubs.ps1" $config["EVENTHUBS_NAMESPACE"] $config["EVENTHUBS_ENTITY_PATH"] $config["EVENTHUBS_USERNAME"]
        if(-not [String]::IsNullOrWhiteSpace($ehPassword))
        {
            & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{EVENTHUBS_PASSWORD=$ehPassword}
        }
    }
    $ehJob = Start-Job -Script $scriptCreateEH -Name EventHub -ArgumentList $subName,$scriptDir,$configFile,$config
}

if($docdb)
{
    Write-SpecialLog "Creating DocumentDB and update account key in configurations.properties" (Get-ScriptName) (Get-ScriptLineNumber)
    $scriptCreateDocDb = {
        param($subName,$scriptDir,$configFile,$config)
        Select-AzureSubscription -SubscriptionName $subName
        & "$scriptDir\..\init.ps1"
        $docdbKey = & "$scriptDir\DocumentDB\CreateDocumentDB.ps1" $config["DOCUMENTDB_ACCOUNT"]
        if(-not [String]::IsNullOrWhiteSpace($docdbKey))
        {
            & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{DOCDB_KEY=$docdbKey}
        }
    }
    $docdbJob = Start-Job -Script $scriptCreateDocDb -Name DocDb -ArgumentList $subName,$scriptDir,$configFile,$config
}

Write-SpecialLog "Step 3: Creating HDInsight Clusters" (Get-ScriptName) (Get-ScriptLineNumber)
$scriptCreateStorm = {
    param($subName,$scriptDir,$configFile,$config)
    Select-AzureSubscription -SubscriptionName $subName
    & "$scriptDir\..\init.ps1"
    $cluster = & "$scriptDir\HDInsight\CreateCluster.ps1" $config["STORM_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] "Storm"
    if(-not [String]::IsNullOrWhiteSpace($cluster.ConnectionUrl))
    {
        & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{STORM_CLUSTER_URL=$cluster.ConnectionUrl}
    }
}
$stormJob = Start-Job -Script $scriptCreateStorm -Name Storm -ArgumentList $subName,$scriptDir,$configFile,$config

if($hbase)
{
    $scriptCreateHBase = {
        param($subName,$scriptDir,$configFile,$config)
        Select-AzureSubscription -SubscriptionName $subName
        & "$scriptDir\..\init.ps1"
        $cluster = & "$scriptDir\HDInsight\CreateCluster.ps1" $config["HBASE_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["HBASE_CLUSTER_USERNAME"] $config["HBASE_CLUSTER_PASSWORD"] "HBase"
        if(-not [String]::IsNullOrWhiteSpace($cluster.ConnectionUrl))
        {
            & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{HBASE_CLUSTER_URL=$cluster.ConnectionUrl}
        }
    }
    $hbaseJob = Start-Job -Script $scriptCreateHBase -Name HBase -ArgumentList $subName,$scriptDir,$configFile,$config
}

$jobs = Get-Job -State "Running"
While ($jobs)
{
    $jobsInfo = $jobs | % { "`r`nId = " + $_.Id + ", Name = " + $_.Name + ", State = " + $_.State }
    Write-InfoLog "Currently running Jobs:$jobsInfo" (Get-ScriptName) (Get-ScriptLineNumber)
    sleep -s 10
    if($stormJob -ne $null)
    {
        Get-Job -Id $stormJob.Id | Receive-Job
    }
    if($eventhub -and ($ehJob -ne $null))
    {
        Get-Job -Id $ehJob.Id | Receive-Job
    }
    if($docdb -and ($docdbJob -ne $null))
    {
        Get-Job -Id $docdbJob.Id | Receive-Job
    }
    if($hbase -and ($hbaseJob -ne $null))
    {
        Get-Job -Id $hbaseJob.Id | Receive-Job
    }
    $jobs = Get-Job -State "Running"
}

if($stormJob -ne $null)
{
    Get-Job -Id $stormJob.Id | Remove-Job
}
if($eventhub -and ($ehJob -ne $null))
{
    Get-Job -Id $ehJob.Id | Remove-Job
}
if($docdb -and ($docdbJob -ne $null))
{
    Get-Job -Id $docdbJob.Id | Remove-Job
}
if($hbase -and ($hbaseJob -ne $null))
{
    Get-Job -Id $hbaseJob.Id | Remove-Job
}

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-InfoLog "Azure resources created, completed in $totalSeconds seconds." (Get-ScriptName) (Get-ScriptLineNumber)