[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$Namespace,                             # required    needs to be alphanumeric or '-'
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$Path,                                  # required    needs to be alphanumeric or '-'
    [String]$EventHubsKeyName,                      # required
    [String]$EventHubsPassword,                     # required
    [String]$Location = "West Europe",              # optional    default to "West Europe"
    [Int]$PartitionCount = 8,                      # optional    default to 16
    [Int]$MessageRetentionInDays = 7,               # optional    default to 7
    [String]$UserMetadata = $null,                  # optional    default to $null
    [String]$ConsumerGroupName = "MyConsumerGroup", # optional    default to "MyConsumerGroup"
    [String]$ConsumerGroupUserMetadata = $null,     # optional    default to $null
    [Bool]$CreateACSNamespace = $False              # optional    default to $false
    )

Write-Host "Adding the [Microsoft.ServiceBus.dll] assembly to the script..."
Add-Type -Path "$(Split-Path $script:MyInvocation.MyCommand.Path)\Microsoft.ServiceBus.dll"
Write-Host "The [Microsoft.ServiceBus.dll] assembly has been successfully added to the script."

$startTime = Get-Date

# Create Azure Service Bus namespace
$CurrentNamespace = Get-AzureSBNamespace -Name $Namespace


# Check if the namespace already exists or needs to be created
if ($CurrentNamespace)
{
    Write-Host "The namespace [$Namespace] already exists in the [$($CurrentNamespace.Region)] region." 
}
else
{
    Write-Host "The [$Namespace] namespace does not exist."
    Write-Host "Creating the [$Namespace] namespace in the [$Location] region..."
    New-AzureSBNamespace -Name $Namespace -Location $Location -CreateACSNamespace $CreateACSNamespace -NamespaceType Messaging
    #introduce a delay so that the namespace info can be retrieved
    sleep -s 10
    $CurrentNamespace = Get-AzureSBNamespace -Name $Namespace
    Write-Host "The [$Namespace] namespace in the [$Location] region has been successfully created."
}

# Create the NamespaceManager object to create the event hub
Write-Host "Creating a NamespaceManager object for the [$Namespace] namespace..."
$NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($CurrentNamespace.ConnectionString);
Write-Host "NamespaceManager object for the [$Namespace] namespace has been successfully created."

# Check if the event hub already exists
if ($NamespaceManager.EventHubExists($Path))
{
    Write-Host "The [$Path] event hub already exists in the [$Namespace] namespace." 
}
else
{
    Write-Host "Creating the [$Path] event hub in the [$Namespace] namespace: PartitionCount=[$PartitionCount] MessageRetentionInDays=[$MessageRetentionInDays]..."
    $EventHubDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.EventHubDescription -ArgumentList $Path
    $EventHubDescription.PartitionCount = $PartitionCount
    $EventHubDescription.MessageRetentionInDays = $MessageRetentionInDays
    $EventHubDescription.UserMetadata = $UserMetadata
    #ehd.Authorization.Add(new SharedAccessAuthorizationRule(ruleName, ruleKey, new AccessRights[] { AccessRights.Manage, AccessRights.Listen, AccessRights.Send }));
    $MyAccessRights = [Microsoft.ServiceBus.Messaging.AccessRights[]]([Microsoft.ServiceBus.Messaging.AccessRights]::Manage,[Microsoft.ServiceBus.Messaging.AccessRights]::Send,[Microsoft.ServiceBus.Messaging.AccessRights]::Listen)
    $Rule = New-Object -TypeName Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule -ArgumentList $EventHubsKeyName,$EventHubsPassword,$MyAccessRights
    $EventHubDescription.Authorization.Add($Rule)
    $NamespaceManager.CreateEventHub($EventHubDescription);
    Write-Host "The [$Path] event hub in the [$Namespace] namespace has been successfully created."
}

# Create the consumer group if not exists
Write-Host "Creating the consumer group [$ConsumerGroupName] for the [$Path] event hub..."
$ConsumerGroupDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.ConsumerGroupDescription -ArgumentList $Path, $ConsumerGroupName
$ConsumerGroupDescription.UserMetadata = $ConsumerGroupUserMetadata
$NamespaceManager.CreateConsumerGroupIfNotExists($ConsumerGroupDescription);
Write-Host "The consumer group [$ConsumerGroupName] for the [$Path] event hub has been successfully created."

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-Host "CreateEventHubs completed in $totalSeconds seconds."