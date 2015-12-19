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

try
{
    $account = Get-AzureRmContext
}
catch {}

if($account -eq $null)
{
    $account = Add-AzureRmAccount
    if($account -eq $null)
    {
        Write-ErrorLog "Failed to add Azure RM Account." (Get-ScriptName) (Get-ScriptLineNumber)
        throw "Failed to add Azure RM Account."
    }
}
Write-SpecialLog ("Using Azure RM Account: " + $account.Name) (Get-ScriptName) (Get-ScriptLineNumber)

$subscriptions = Get-AzureRmSubscription
$subId = ($subscriptions | ? { $_.SubscriptionId -eq $config["AZURE_SUBSCRIPTION_ID"] } | Select-Object -First 1 ).SubscriptionId
if($subId -eq $null)
{
    Write-InfoLog ("Available Subscription Names:" + ($subscriptions | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)

    $subId = Read-Host "Enter Azure Subscription Name or Id)"

    $subscription = $subscriptions | ? { ($_.SubscriptionName -eq $subId) -or ($_.SubscriptionId -eq $subId) } | Select-Object -First 1
    #Update the Azure Subscription Id in config
    & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile `
    @{
        AZURE_SUBSCRIPTION_NAME=$subscription.SubscriptionName
        AZURE_SUBSCRIPTION_ID=$subscription.SubscriptionId
        AZURE_TENANT_ID=$subscription.TenantId
    }

    $location = Read-Host "Enter Azure Location, hit enter for default (West Europe)"
    if([String]::IsNullOrWhiteSpace($location))
    {
        $location = "West Europe"
    }
    
    #Update the Azure Location in config
    & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{AZURE_LOCATION=$location}
    
    $osType = Read-Host "Enter HDInsight OS Choice (Windows or Linux), hit enter for default (Windows)"
    if([String]::IsNullOrWhiteSpace($osType))
    {
        $osType = "Windows"
    }
    #Update the HDInsight OS Type in config
    & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{HDINSIGHT_CLUSTER_OS_TYPE=$osType}
    
    if($osType -eq "Linux")
    {
        & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{VNET_VERSION="ARM"}
    }
    else
    {
        & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{VNET_VERSION="Classic"}
    }

    $clusterSize = Read-Host "Specify HDInsight Cluster Size in Nodes, hit enter for default (2)"
    if([String]::IsNullOrWhiteSpace($clusterSize))
    {
        $clusterSize = "2"
    }
    #Update the HDInsight Cluster Size and event hub partition count in config
    & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{HDINSIGHT_CLUSTER_SIZE=$clusterSize}
    
    ###########################################################
    # Refresh Run Configuration
    ###########################################################
    $config = & "$scriptDir\..\config\ReadConfig.ps1" $configFile
}

Write-SpecialLog "Current run configuration:" (Get-ScriptName) (Get-ScriptLineNumber)
$config.Keys | sort | % { if(-not ($_.Contains("PASSWORD") -or $_.Contains("KEY"))) { Write-SpecialLog ("Key = " + $_ + ", Value = " + $config[$_]) (Get-ScriptName) (Get-ScriptLineNumber) } }

Write-SpecialLog ("Using subscription: {0} - {1}" -f $config["AZURE_SUBSCRIPTION_NAME"], $config["AZURE_SUBSCRIPTION_ID"]) (Get-ScriptName) (Get-ScriptLineNumber)
Set-AzureRmContext -TenantId $config["AZURE_TENANT_ID"] -SubscriptionId $config["AZURE_SUBSCRIPTION_ID"]

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

Write-SpecialLog "Step 0: Creating Azure Resource Group" (Get-ScriptName) (Get-ScriptLineNumber)
& "$scriptDir\CreateAzureResourceGroup.ps1" $config["AZURE_RESOURCE_GROUP"] $config["AZURE_LOCATION"]

if($vnet)
{
    Write-SpecialLog "Step 1: Creating Azure Virtual Network" (Get-ScriptName) (Get-ScriptLineNumber)
    if($config["VNET_VERSION"] -eq "ARM")
    {
        $vnetId = & "$scriptDir\VirtualNetwork\CreateVirtualNetworkARM.ps1" $config["AZURE_RESOURCE_GROUP"] $config["AZURE_LOCATION"] $config["VNET_NAME"] $config["SUBNET_NAME"]
    }
    else
    {
        $VNetConfigFilePath = Join-Path $ExampleDir ("run\" + $config["VNET_NAME"] + ".netcfg")
        $vnetId = & "$scriptDir\VirtualNetwork\CreateVNet.ps1" $VNetConfigFilePath $config["AZURE_LOCATION"] $config["VNET_NAME"] $config["SUBNET_NAME"]
        if([String]::IsNullOrWhiteSpace($vnetId))
        {
            throw "Cannot get VNet Id"
        }
    }
    & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{VNET_ID=$vnetId}
    $config["VNET_ID"] = $vnetId
}

Write-SpecialLog "Step 2: Creating storage account and updating key in configurations.properties" (Get-ScriptName) (Get-ScriptLineNumber)
$storageKey = & "$scriptDir\Storage\CreateStorageAccountARM.ps1" $config["AZURE_RESOURCE_GROUP"] $config["AZURE_LOCATION"] $config["WASB_ACCOUNT_NAME"]
if([String]::IsNullOrWhiteSpace($storageKey))
{
    throw "Cannot get Storage Key"
}
else
{
    & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{WASB_ACCOUNT_KEY=$storageKey}
    $config["WASB_ACCOUNT_KEY"] = $storageKey
}

#add a small delay here in order for dependent resources to get the storage account
sleep -s 15

if($sqlAzure)
{
    #Let's attempt to create SQL Azure first than trying in parallel as it's critical to not lose information as server name is generated during that
    #This can lead to leaks of new SQL Azure Servers on re-runs if previous task failed to save the value
    Write-SpecialLog "Step 2.1: Creating SQL Azure Server and updating values in configurations.properties" (Get-ScriptName) (Get-ScriptLineNumber)
    $sqlServerName = & "$scriptDir\SqlAzure\CreateSqlAzureARM.ps1" $config["AZURE_RESOURCE_GROUP"] $config["AZURE_LOCATION"] `
        $config["SQLAZURE_SERVER_NAME"] $config["SQLAZURE_DB_NAME"] $config["SQLAZURE_USER"] $config["SQLAZURE_PASSWORD"]
    if(-not [String]::IsNullOrWhiteSpace($sqlServerName))
    {
        & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{SQLAZURE_SERVER_NAME=$sqlServerName}
    }
}

if($eventhub)
{
    Write-SpecialLog "Step 2.2: Creating Event Hubs" (Get-ScriptName) (Get-ScriptLineNumber)
   
    $ehPassword = & "$scriptDir\EventHubs\CreateEventHubs.ps1" $config["EVENTHUBS_NAMESPACE"] $config["EVENTHUBS_ENTITY_PATH"] $config["EVENTHUBS_USERNAME"] `
        $config["AZURE_LOCATION"] $config["EVENTHUBS_PARTITION_COUNT"]
    
    if(-not [String]::IsNullOrWhiteSpace($ehPassword))
    {
        & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{EVENTHUBS_PASSWORD=$ehPassword}
    }
}


if($docdb)
{
    Write-SpecialLog "Step 2.3: Creating DocumentDB and update account key in configurations.properties" (Get-ScriptName) (Get-ScriptLineNumber)
    $docdbKey = & "$scriptDir\DocumentDB\CreateDocumentDBARM.ps1" $config["AZURE_RESOURCE_GROUP"] $config["AZURE_LOCATION"] $config["DOCUMENTDB_ACCOUNT"]
    if(-not [String]::IsNullOrWhiteSpace($docdbKey))
    {
        & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{DOCDB_KEY=$docdbKey}
    }
}

Write-SpecialLog "Step 4: Creating HDInsight Clusters" (Get-ScriptName) (Get-ScriptLineNumber)

if($vnet)
{
    $vnetId = $config["VNET_ID"]
    $subnet = $config["SUBNET_NAME"]
}

$cluster = & "$scriptDir\HDInsight\CreateClusterARM.ps1" $config["AZURE_RESOURCE_GROUP"] $config["AZURE_LOCATION"] $config["STORM_CLUSTER_NAME"] `
    $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] "Storm" $config["STORM_CLUSTER_OS_TYPE"] `
    $config["STORM_CLUSTER_SIZE"] $vnetId $subnet
    
if(-not [String]::IsNullOrWhiteSpace($cluster.HttpEndpoint))
{
    & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{STORM_CLUSTER_URL=("https://" + $cluster.HttpEndpoint)}
}


if($hbase)
{
    $cluster = & "$scriptDir\HDInsight\CreateClusterARM.ps1" $config["AZURE_RESOURCE_GROUP"] $config["AZURE_LOCATION"] $config["HBASE_CLUSTER_NAME"] `
        $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["HBASE_CLUSTER_USERNAME"] $config["HBASE_CLUSTER_PASSWORD"] "HBase" $config["HBASE_CLUSTER_OS_TYPE"] `
        $config["HBASE_CLUSTER_SIZE"] $vnetId $subnet

    if(-not [String]::IsNullOrWhiteSpace($cluster.HttpEndpoint))
    {
        & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{HBASE_CLUSTER_URL=("https://" + $cluster.HttpEndpoint)}
    }
}

if($kafka)
{
    if($config["KAFKA_CLUSTER_OS_TYPE"] -eq "Linux")
    {
        $cluster = & "$scriptDir\HDInsight\CreateClusterARM.ps1" $config["AZURE_RESOURCE_GROUP"] $config["AZURE_LOCATION"] $config["KAFKA_CLUSTER_NAME"] `
            $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["KAFKA_CLUSTER_USERNAME"] $config["KAFKA_CLUSTER_PASSWORD"] "Storm" $config["KAFKA_CLUSTER_OS_TYPE"] `
            $config["KAFKA_CLUSTER_SIZE"] $vnetId $subnet
            
        if(-not [String]::IsNullOrWhiteSpace($cluster.HttpEndpoint))
        {
            & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{KAFKA_CLUSTER_URL=("https://" + $cluster.HttpEndpoint)}
        }
    }
    else
    {
        $unzipUri = & "$scriptDir\Storage\UploadFileToStorage.ps1" $config["AZURE_RESOURCE_GROUP"] $config["WASB_ACCOUNT_NAME"] `
            "kafkaconfigactionv03" "$scriptDir\HDInsight\Kafka\unzip.exe" "unzip.exe"
        Write-InfoLog "unzipUri: $unzipUri" (Get-ScriptName) (Get-ScriptLineNumber)
        $kafkaVersion = "kafka_2.11-0.8.2.1"
        $kafkaUri = & "$scriptDir\Storage\UploadFileToStorage.ps1" $config["AZURE_RESOURCE_GROUP"] $config["WASB_ACCOUNT_NAME"] `
            "kafkaconfigactionv03" "$scriptDir\HDInsight\Kafka\$kafkaVersion.zip" "$kafkaVersion.zip"
        Write-InfoLog "KafkaUri: $kafkaUri" (Get-ScriptName) (Get-ScriptLineNumber)
        $ScriptActionUri = & "$scriptDir\Storage\UploadFileToStorage.ps1" $config["AZURE_RESOURCE_GROUP"] $config["WASB_ACCOUNT_NAME"] `
            "kafkaconfigactionv03" "$scriptDir\HDInsight\Kafka\kafka-installer-v03.ps1" "kafka-installer-v03.ps1"
        Write-InfoLog "ScriptActionUri: $ScriptActionUri" (Get-ScriptName) (Get-ScriptLineNumber)
        $ScriptActionParameters = "-KafkaBinaryZipLocation $kafkaUri -KafkaHomeName $kafkaVersion -UnzipExeLocation $unzipUri -RemoteAdminUsername remote{0} -RemoteAdminPassword {1}" `
            -f $config["KAFKA_CLUSTER_USERNAME"], $config["KAFKA_CLUSTER_PASSWORD"]
        Write-InfoLog "ScriptActionParameters: $ScriptActionParameters" (Get-ScriptName) (Get-ScriptLineNumber)
        
        $cluster = & "$scriptDir\HDInsight\CreateClusterARM.ps1" $config["AZURE_RESOURCE_GROUP"] $config["AZURE_LOCATION"] $config["KAFKA_CLUSTER_NAME"] `
            $config["WASB_ACCOUNT_NAME"] $config["WASB_CONTAINER"] $config["KAFKA_CLUSTER_USERNAME"] $config["KAFKA_CLUSTER_PASSWORD"] "Storm" $config["KAFKA_CLUSTER_OS_TYPE"] `
            $config["KAFKA_CLUSTER_SIZE"] $vnetId $subnet `
            -ScriptActionUri $ScriptActionUri -ScriptActionParameters $ScriptActionParameters
            
        if(-not [String]::IsNullOrWhiteSpace($cluster.ConnectionUrl))
        {
            & "$scriptDir\..\config\ReplaceStringInFile.ps1" $configFile $configFile @{KAFKA_CLUSTER_URL=$cluster.ConnectionUrl}            
        }
    }
}

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-InfoLog "Azure resources created, completed in $totalSeconds seconds." (Get-ScriptName) (Get-ScriptLineNumber)
