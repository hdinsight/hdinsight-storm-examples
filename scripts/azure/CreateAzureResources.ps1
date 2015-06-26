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

###########################################################
# Get Run Configuration
###########################################################
$configFile = Join-Path $ExampleDir "run\configurations.properties"
# Make sure you run this in Microsoft Azure Powershell prompt
if(-not (Test-Path $configFile))
{
    Write-ErrorLog "No run configuration file found at '$configFile'" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "No run configuration file found at '$configFile'"
}
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

$subscriptions = Get-AzureSubscription
$subName = ($subscriptions | ? { $_.SubscriptionName -eq $config["AZURE_SUBSCRIPTION_NAME"] } | Select-Object -First 1 ).SubscriptionName
if($subName -eq $null)
{
    $subNames = $subscriptions | % { "`r`n" + $_.SubscriptionName + " - " + $_.SubscriptionId}
    Write-InfoLog ("Available Subscription Names (Name - Id):" + $subNames) (Get-ScriptName) (Get-ScriptLineNumber)

    $subName = Read-Host "Enter subscription name"

    #Update the Azure Subscription Id in config
    & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{AZURE_SUBSCRIPTION_NAME=$subName}
    
    ###########################################################
    # Refresh Run Configuration
    ###########################################################
    $config = & "$scriptDir\..\config\ReadConfig.ps1" $configFile
}

Write-SpecialLog "Current run configuration:" (Get-ScriptName) (Get-ScriptLineNumber)
$config.Keys | sort | % { if(-not ($_.Contains("PASSWORD") -or $_.Contains("KEY"))) { Write-SpecialLog ("Key = " + $_ + ", Value = " + $config[$_]) (Get-ScriptName) (Get-ScriptLineNumber) } }

Write-SpecialLog ("Using subscription: " + $config["AZURE_SUBSCRIPTION_NAME"]) (Get-ScriptName) (Get-ScriptLineNumber)
Select-AzureSubscription -SubscriptionName $config["AZURE_SUBSCRIPTION_NAME"]

###########################################################
# Check Azure Resource Creation List
###########################################################


$startTime = Get-Date

$vnet = $false
if($config["VNET"].Equals("true", [System.StringComparison]::OrdinalIgnoreCase))
{
    $vnet = $true
}

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

###########################################################
# Create Azure Resources
###########################################################

if($vnet)
{
	Write-SpecialLog "Step 0: Creating Azure Virtual Network" (Get-ScriptName) (Get-ScriptLineNumber)
	$VNetConfigFilePath = Join-Path $ExampleDir ("run\" + $config["VNET_NAME"] + ".netcfg")
	$vnetId = & "$scriptDir\VirtualNetwork\CreateVNet.ps1" $VNetConfigFilePath $config["VNET_NAME"] $config["AZURE_LOCATION"]
	if([String]::IsNullOrWhiteSpace($vnetId))
	{
		throw "Cannot get VNet Id"
	}
	else
	{
		& "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{VNET_ID=$vnetId}
	}
}

Write-SpecialLog "Step 1: Creating storage account and updating key in configurations.properties" (Get-ScriptName) (Get-ScriptLineNumber)
$storageKey = & "$scriptDir\Storage\CreateStorageAccount.ps1" $config["WASB_ACCOUNT_NAME"] $config["AZURE_LOCATION"]
if([String]::IsNullOrWhiteSpace($storageKey))
{
    throw "Cannot get Storage Key"
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
    $sqlServerName = & "$scriptDir\SqlAzure\CreateSqlAzure.ps1" $config["SQLAZURE_SERVER_NAME"] $config["SQLAZURE_DB_NAME"] $config["SQLAZURE_USER"] $config["SQLAZURE_PASSWORD"] $config["AZURE_LOCATION"]
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
        $ehPassword = & "$scriptDir\EventHubs\CreateEventHubs.ps1" $config["EVENTHUBS_NAMESPACE"] $config["EVENTHUBS_ENTITY_PATH"] $config["EVENTHUBS_USERNAME"] $config["AZURE_LOCATION"] $config["EVENTHUBS_PARTITION_COUNT"]
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
        $docdbKey = & "$scriptDir\DocumentDB\CreateDocumentDB.ps1" $config["DOCUMENTDB_ACCOUNT"] $config["AZURE_LOCATION"]
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
	
    if($config["VNET"].Equals("true", [System.StringComparison]::OrdinalIgnoreCase))
    {
        $cluster = & "$scriptDir\HDInsight\CreateCluster.ps1" $config["STORM_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] "Storm" $config["STORM_CLUSTER_SIZE"] $config["VNET_ID"] "Subnet-1"
    }
    else
    {
        $cluster = & "$scriptDir\HDInsight\CreateCluster.ps1" $config["STORM_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] "Storm" $config["STORM_CLUSTER_SIZE"]
	}
	
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
        if($config["VNET"].Equals("true", [System.StringComparison]::OrdinalIgnoreCase))
        {
            $cluster = & "$scriptDir\HDInsight\CreateCluster.ps1" $config["HBASE_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["HBASE_CLUSTER_USERNAME"] $config["HBASE_CLUSTER_PASSWORD"] "HBase" $config["HBASE_CLUSTER_SIZE"] $config["VNET_ID"] "Subnet-1"
        }
        else
        {
            $cluster = & "$scriptDir\HDInsight\CreateCluster.ps1" $config["HBASE_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["HBASE_CLUSTER_USERNAME"] $config["HBASE_CLUSTER_PASSWORD"] "HBase" $config["HBASE_CLUSTER_SIZE"]
        }
        if(-not [String]::IsNullOrWhiteSpace($cluster.ConnectionUrl))
        {
            & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{HBASE_CLUSTER_URL=$cluster.ConnectionUrl}
        }
    }
    $hbaseJob = Start-Job -Script $scriptCreateHBase -Name HBase -ArgumentList $subName,$scriptDir,$configFile,$config
}

if($kafka)
{
    $scriptCreateKafka = {
        param($subName,$scriptDir,$configFile,$config)
        Select-AzureSubscription -SubscriptionName $subName
        & "$scriptDir\..\init.ps1"
        $unzipUri = & "$scriptDir\Storage\UploadFileToStorage.ps1" $config["WASB_ACCOUNT_NAME"] "kafkaconfigactionv02" "$scriptDir\HDInsight\Kafka\unzip.exe" "unzip.exe"
        Write-InfoLog "unzipUri: $unzipUri" (Get-ScriptName) (Get-ScriptLineNumber)
        $kafkaVersion = "kafka_2.11-0.8.2.1"
        $kafkaUri = & "$scriptDir\Storage\UploadFileToStorage.ps1" $config["WASB_ACCOUNT_NAME"] "kafkaconfigactionv02" "$scriptDir\HDInsight\Kafka\$kafkaVersion.zip" "$kafkaVersion.zip"
        Write-InfoLog "KafkaUri: $kafkaUri" (Get-ScriptName) (Get-ScriptLineNumber)
        $ScriptActionUri = & "$scriptDir\Storage\UploadFileToStorage.ps1" $config["WASB_ACCOUNT_NAME"] "kafkaconfigactionv02" "$scriptDir\HDInsight\Kafka\kafka-installer-v02.ps1" "kafka-installer-v02.ps1"
        Write-InfoLog "ScriptActionUri: $ScriptActionUri" (Get-ScriptName) (Get-ScriptLineNumber)
        $ScriptActionParameters = "-KafkaBinaryZipLocation $kafkaUri -KafkaHomeName $kafkaVersion -UnzipExeLocation $unzipUri -RemoteAdminUsername remote{0} -RemoteAdminPassword {1}" -f $config["KAFKA_CLUSTER_USERNAME"], $config["KAFKA_CLUSTER_PASSWORD"]
        Write-InfoLog "ScriptActionParameters: $ScriptActionParameters" (Get-ScriptName) (Get-ScriptLineNumber)
        if($config["VNET"].Equals("true", [System.StringComparison]::OrdinalIgnoreCase))
        {
            $cluster = & "$scriptDir\HDInsight\CreateCluster.ps1" $config["KAFKA_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["KAFKA_CLUSTER_USERNAME"] $config["KAFKA_CLUSTER_PASSWORD"] "Storm" $config["KAFKA_CLUSTER_SIZE"] $config["VNET_ID"] "Subnet-1" $ScriptActionUri $ScriptActionParameters
        }
        else
        {
            $cluster = & "$scriptDir\HDInsight\CreateCluster.ps1" $config["KAFKA_CLUSTER_NAME"] $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["KAFKA_CLUSTER_USERNAME"] $config["KAFKA_CLUSTER_PASSWORD"] "Storm" $config["KAFKA_CLUSTER_SIZE"] "" "" $ScriptActionUri $ScriptActionParameters
        }
        if(-not [String]::IsNullOrWhiteSpace($cluster.ConnectionUrl))
        {
            & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{KAFKA_CLUSTER_URL=$cluster.ConnectionUrl}
        }
    }
    $kafkaJob = Start-Job -Script $scriptCreateKafka -Name Kafka -ArgumentList $subName,$scriptDir,$configFile,$config
}

$jobs = Get-Job -State "Running"
While ($jobs)
{
    $jobsInfo = $jobs | % { "`r`nJob Id = " + $_.Id + ", Job Name = " + $_.Name + ", Job State = " + $_.State }
    Write-InfoLog "Currently running Jobs:$jobsInfo" (Get-ScriptName) (Get-ScriptLineNumber)
    sleep -s 10
    if($stormJob -ne $null)
    {
        $jobOut = Get-Job -Id $stormJob.Id | Receive-Job
        if($jobOut)
        {
            Write-InfoLog $jobOut (Get-ScriptName) (Get-ScriptLineNumber)
        }
    }
    if($eventhub -and ($ehJob -ne $null))
    {
        $jobOut = Get-Job -Id $ehJob.Id | Receive-Job
        if($jobOut)
        {
            Write-InfoLog $jobOut (Get-ScriptName) (Get-ScriptLineNumber)
        }
    }
    if($docdb -and ($docdbJob -ne $null))
    {
        $jobOut = Get-Job -Id $docdbJob.Id | Receive-Job
        if($jobOut)
        {
            Write-InfoLog $jobOut (Get-ScriptName) (Get-ScriptLineNumber)
        }
    }
    if($hbase -and ($hbaseJob -ne $null))
    {
        $jobOut = Get-Job -Id $hbaseJob.Id | Receive-Job
        if($jobOut)
        {
            Write-InfoLog $jobOut (Get-ScriptName) (Get-ScriptLineNumber)
        }
    }
    if($kafka -and ($kafkaJob -ne $null))
    {
        $jobOut = Get-Job -Id $kafkaJob.Id | Receive-Job
        if($jobOut)
        {
            Write-InfoLog $jobOut (Get-ScriptName) (Get-ScriptLineNumber)
        }
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
if($kafka -and ($kafkaJob -ne $null))
{
    Get-Job -Id $kafkaJob.Id | Remove-Job
}

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-InfoLog "Azure resources created, completed in $totalSeconds seconds." (Get-ScriptName) (Get-ScriptLineNumber)