#returns account primary key when successful otherwise $null
[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$AccountName,                    # required    needs to be alphanumeric or "-"
    [String]$Location = "West Europe",       # optional
    [String]$OfferType = "Standard"          # optional    defaults to Standard
    )

$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

#Switch to Azure resource manager mode
$result = Switch-AzureMode -Name AzureResourceManager

$ResourceGroupName="{0}{1}" -f $AccountName,"Group"
$ResourceType="Microsoft.DocumentDb/databaseAccounts"
$ApiVersion="2015-04-08"

$startTime = Get-Date

$resourceGroupStatus = $false
$resourceStatus = $false

try
{
    Write-InfoLog "Trying to find AzureResourceGroup: $ResourceGroupName" (Get-ScriptName) (Get-ScriptLineNumber)
    $ResourceGroup = Get-AzureResourceGroup -Name $ResourceGroupName
    $resourceGroupStatus = $true
}
catch 
{
    Write-WarnLog "Could not find ResourceGroup account: $ResourceGroupName. Will attempt to create a new one." (Get-ScriptName) (Get-ScriptLineNumber)
}

if($ResourceGroup -eq $null)
{
    try
    {
        $ResourceGroup = New-AzureResourceGroup -Name $ResourceGroupName -Location "$Location" -Force
    }
    catch
    {
        Write-ErrorLog "Failed to create AzureResourceGroup." (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}

Write-InfoLog ("AzureResourceGroup Information:`r`n" + ($ResourceGroup | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
        
try
{
    Write-InfoLog "Trying to find DocumentDB account: $AccountName in ResourceGroup: $ResourceGroupName" (Get-ScriptName) (Get-ScriptLineNumber)
    $Resource = Get-AzureResource -Name $AccountName -ResourceGroupName $ResourceGroupName -ResourceType $ResourceType -ApiVersion $ApiVersion -OutputObjectFormat New
    $resourceStatus = $true
}
catch 
{
    Write-WarnLog "Could not find DocumentDB account: $AccountName. Will attempt to create a new one." (Get-ScriptName) (Get-ScriptLineNumber)
}

if($Resource -eq $null)
{
    Write-InfoLog "Creating DocumentDB account" (Get-ScriptName) (Get-ScriptLineNumber)
    try
    {
        $docDbProperties = @{name = "$AccountName"; "id" = "$AccountName"; "databaseAccountOfferType" = "$OfferType"; }
        Write-InfoLog ("DocumentDB Properties:`r`n" + ($docDbProperties | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
        
        $Resource = New-AzureResource -Name $AccountName -Location "$Location" -ResourceGroupName $ResourceGroupName -ResourceType $ResourceType -ApiVersion $ApiVersion -PropertyObject $docDbProperties -Force -OutputObjectFormat New
        $iterCount = 30
        $iter = 1
        while($iter -le $iterCount)
        {
            $Resource = Get-AzureResource -Name $AccountName -ResourceGroupName $ResourceGroupName -ResourceType $ResourceType -ApiVersion $ApiVersion -OutputObjectFormat New
            $CurrentState = $Resource.Properties['provisioningState']
            Write-InfoLog "DocumentDB current state: [$CurrentState]" (Get-ScriptName) (Get-ScriptLineNumber)
            if("succeeded" -eq $CurrentState) 
            {
                $resourceStatus = $true
                break
            }
            Start-Sleep -s 30
        }
    }
    catch
    {
        Write-ErrorLog "Failed to create DocumentDB." (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }

    #Later you can modify the account properties like this:
    #$DocumentDBProperties = @{capacityUnits="3", “consistencyPolicy={“defaultConsistencyLevel”=”0”}}
    #Set-AzureResource -Name DocDBAccountName -ResourceGroupName MyResourceGroup -ResourceType "Microsoft.DocumentDb/databaseAccounts" -ApiVersion $ApiVersion -PropertyObject $DocumentDBProperties
}

Write-InfoLog ("DocumentDB Information:`r`n" + ($Resource | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)

if($resourceStatus)
{
    Write-InfoLog "Found DocumentDB account information" (Get-ScriptName) (Get-ScriptLineNumber)
    Write-InfoLog "Getting account key" (Get-ScriptName) (Get-ScriptLineNumber)
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
    $keysurl = [System.String]::Format("https://management.azure.com/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.DocumentDB/databaseAccounts/{2}/listKeys?api-version=$ApiVersion", $sub.SubscriptionId, $ResourceGroupName, $AccountName)
    $keys = Invoke-RestMethod -Method POST -Uri $keysurl -Headers @{"Authorization"=$header} -ContentType "application/json"
    $keys.primaryMasterKey
    
    $finishTime = Get-Date
    $totalSeconds = ($finishTime - $startTime).TotalSeconds
    Write-InfoLog "CreateDocumentDB completed in $totalSeconds seconds." (Get-ScriptName) (Get-ScriptLineNumber)
}

$result = Switch-AzureMode -Name AzureServiceManagement

if(-not $resourceStatus)
{
    Write-ErrorLog "Failed to create DocumentDB account" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Failed to create DocumentDB account"
}