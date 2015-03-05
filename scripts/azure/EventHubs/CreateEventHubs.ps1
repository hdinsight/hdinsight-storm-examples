[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$Namespace,                             # required    needs to be alphanumeric or '-'
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$Path,                                  # required    needs to be alphanumeric or '-'
    [String]$EventHubsKeyName,                      # required
    [String]$Location = "West Europe",              # optional    default to "West Europe"
    [Int]$PartitionCount = 16,                      # optional    default to 16
    [Int]$MessageRetentionInDays = 7,               # optional    default to 7
    [String]$UserMetadata = $null,                  # optional    default to $null
    [String]$ConsumerGroupName = "MyConsumerGroup", # optional    default to "MyConsumerGroup"
    [String]$ConsumerGroupUserMetadata = $null,     # optional    default to $null
    [Bool]$CreateACSNamespace = $False              # optional    default to $false
    )

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath
$serviceBusDll = & "$scriptDir\GetServiceBusDll.ps1" (Get-ScriptName) (Get-ScriptLineNumber)

Write-InfoLog "Adding the $serviceBusDll assembly to the script..." (Get-ScriptName) (Get-ScriptLineNumber)
Add-Type -Path $serviceBusDll
Write-InfoLog "The $serviceBusDll assembly has been successfully added to the script." (Get-ScriptName) (Get-ScriptLineNumber)

$startTime = Get-Date

# Create Azure Service Bus namespace
$CurrentNamespace = Get-AzureSBNamespace -Name $Namespace

# Check if the namespace already exists or needs to be created
if ($CurrentNamespace)
{
    Write-InfoLog "The namespace [$Namespace] already exists in the [$($CurrentNamespace.Region)] region." (Get-ScriptName) (Get-ScriptLineNumber)
}
else
{
    Write-InfoLog "The [$Namespace] namespace does not exist." (Get-ScriptName) (Get-ScriptLineNumber)
    Write-InfoLog "Creating the [$Namespace] namespace in the [$Location] region..." (Get-ScriptName) (Get-ScriptLineNumber)
    $CurrentNamespace = New-AzureSBNamespace -Name $Namespace -Location $Location -CreateACSNamespace $CreateACSNamespace -NamespaceType Messaging
    #introduce a delay so that the namespace info can be retrieved
    sleep -s 15
    $CurrentNamespace = Get-AzureSBNamespace -Name $Namespace
    Write-InfoLog "The [$Namespace] namespace in the [$Location] region has been successfully created." (Get-ScriptName) (Get-ScriptLineNumber)
}

# Create the NamespaceManager object to create the event hub
Write-InfoLog "Creating a NamespaceManager object for the [$Namespace] namespace..." (Get-ScriptName) (Get-ScriptLineNumber)
$NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($CurrentNamespace.ConnectionString);
Write-InfoLog "NamespaceManager object for the [$Namespace] namespace has been successfully created." (Get-ScriptName) (Get-ScriptLineNumber)

$MyAccessRights = [Microsoft.ServiceBus.Messaging.AccessRights[]]([Microsoft.ServiceBus.Messaging.AccessRights]::Manage,[Microsoft.ServiceBus.Messaging.AccessRights]::Send,[Microsoft.ServiceBus.Messaging.AccessRights]::Listen)

# Check if the event hub already exists
if ($NamespaceManager.EventHubExists($Path))
{
    Write-InfoLog "The [$Path] event hub already exists in the [$Namespace] namespace." (Get-ScriptName) (Get-ScriptLineNumber)
    $EventHubDescription = $NamespaceManager.GetEventHub($Path)
    $Rule = New-Object -TypeName Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule -ArgumentList $EventHubsKeyName,$MyAccessRights
    $dummy = $EventHubDescription.Authorization.TryGetSharedAccessAuthorizationRule($EventHubsKeyName, [ref]$Rule)
}
else
{
    Write-InfoLog "Creating the [$Path] event hub in the [$Namespace] namespace: PartitionCount=[$PartitionCount] MessageRetentionInDays=[$MessageRetentionInDays]..." (Get-ScriptName) (Get-ScriptLineNumber)
    $EventHubDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.EventHubDescription -ArgumentList $Path
    $EventHubDescription.PartitionCount = $PartitionCount
    $EventHubDescription.MessageRetentionInDays = $MessageRetentionInDays
    $EventHubDescription.UserMetadata = $UserMetadata
    #ehd.Authorization.Add(new SharedAccessAuthorizationRule(ruleName, ruleKey, new AccessRights[] { AccessRights.Manage, AccessRights.Listen, AccessRights.Send }));
    $EventHubsPassword = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey()
    $Rule = New-Object -TypeName Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule -ArgumentList $EventHubsKeyName,$EventHubsPassword,$MyAccessRights
    $EventHubDescription.Authorization.Add($Rule)
    $EventHubDescription = $NamespaceManager.CreateEventHub($EventHubDescription);
    Write-InfoLog "The [$Path] event hub in the [$Namespace] namespace has been successfully created." (Get-ScriptName) (Get-ScriptLineNumber)
}

# Create the consumer group if not exists
Write-InfoLog "Creating the consumer group [$ConsumerGroupName] for the [$Path] event hub..." (Get-ScriptName) (Get-ScriptLineNumber)
$ConsumerGroupDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.ConsumerGroupDescription -ArgumentList $Path, $ConsumerGroupName
$ConsumerGroupDescription.UserMetadata = $ConsumerGroupUserMetadata
$ConsumerGroupDescription = $NamespaceManager.CreateConsumerGroupIfNotExists($ConsumerGroupDescription);
Write-InfoLog "The consumer group [$ConsumerGroupName] for the [$Path] event hub has been successfully created." (Get-ScriptName) (Get-ScriptLineNumber)

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-InfoLog "CreateEventHubs completed in $totalSeconds seconds." (Get-ScriptName) (Get-ScriptLineNumber)

return $Rule.PrimaryKey