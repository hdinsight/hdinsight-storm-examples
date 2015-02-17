#returns account primary key when successful otherwise $null
[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$AccountName,                    # required    needs to be alphanumeric or "-"
    [String]$Location = "West Europe",       # optional
    [Int]$CapacityUnits = 2                  # optional    defaults to 2
    )

$startTime = Get-Date

#Switch to Azure resource manager mode
$result = Switch-AzureMode -Name AzureResourceManager

$ResourceGroupName="{0}{1}" -f $AccountName,"Group"
$ResourceType="Microsoft.DocumentDb/databaseAccounts"
$ApiVersion="2014-07-10"

Write-Host "Creating DocumentDB account"
$result = New-AzureResource -Name $AccountName -Location "$Location" -ResourceGroupName $ResourceGroupName -ResourceType $ResourceType -ApiVersion $ApiVersion -PropertyObject @{"name" = "$AccountName"; "capacityUnits" = "$CapacityUnits"} -Force

#Later you can modify the account properties like this:
#$DocumentDBProperties = @{capacityUnits="3", “consistencyPolicy={“defaultConsistencyLevel”=”0”}}
#Set-AzureResource -Name DocDBAccountName -ResourceGroupName MyResourceGroup -ResourceType "Microsoft.DocumentDb/databaseAccounts" -ApiVersion 2014-07-10 -PropertyObject $DocumentDBProperties

$success = $false
$iterCount = 30
$iter = 1
while($iter -le $iterCount)
{
    $Resource = Get-AzureResource -Name $AccountName -ResourceGroupName $ResourceGroupName -ResourceType $ResourceType -ApiVersion $ApiVersion
    $CurrentState = $Resource.Properties['provisioningState']
    Write-Host "DocumentDB current state: [$CurrentState]"
    if("succeeded" -eq $CurrentState) 
    {
        $success = $true
        break
    }
    Start-Sleep -s 30
}
if($success)
{
    Write-Host "Successfully created DocumentDB account"
    Write-Host "Getting account key"
    #work around to get account key
    $clientId = "1950a258-227b-4e31-a9cf-717495945fc2"
    $redirectUri = "urn:ietf:wg:oauth:2.0:oob"
    $resourceClientId = "00000002-0000-0000-c000-000000000000"
    $resourceAppIdURI = "https://management.core.windows.net/"
    $authority = "https://login.windows.net/common"
    $authContext = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext" -ArgumentList $authority,$false
    $authResult = $authContext.AcquireToken($resourceAppIdURI, $clientId, $redirectUri, "Auto")
    $header = $authresult.CreateAuthorizationHeader()

    $sub = Get-AzureSubscription -Current
    $keysurl = [System.String]::Format("https://management.azure.com/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.DocumentDB/databaseAccounts/{2}/listKeys?api-version=2014-04-01", $sub.SubscriptionId, $ResourceGroupName, $AccountName)
    $keys = Invoke-RestMethod -Method POST -Uri $keysurl -Headers @{"Authorization"=$header} -ContentType "application/json"
    $keys.primaryMasterKey
}
else
{
    Write-Host "Failed to create DocumentDB account"
}

$result = Switch-AzureMode -Name AzureServiceManagement

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-Host "CreateDocumentDB completed in $totalSeconds seconds."