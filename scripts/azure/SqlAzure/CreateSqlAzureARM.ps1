#returns SQL Database server name
[CmdletBinding(PositionalBinding=$true)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$ResourceGroupName,              # required - needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [String]$Location,                       # required
    [Parameter(Mandatory = $true)]
    [String]$ServerName,                     # required - OKAY to pass empty string to create one
    [Parameter(Mandatory = $true)]
    [String]$DatabaseName,                   # required
    [Parameter(Mandatory = $true)]
    [String]$AdminLogin,                     # required
    [Parameter(Mandatory = $true)]
    [String]$AdminPassword                   # required
    )

###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit -9999
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

$startTime = Get-Date

& "$scriptDir\..\CreateAzureResourceGroup.ps1" $ResourceGroupName $Location

#######################################################
# 1. Create Azure SQL Database Server if not exists

if(-not ([String]::IsNullOrWhiteSpace($ServerName)))
{
    #If a bad server name was passed, do not throw as we will create a new one
    try
    {
        $SqlServer = Get-AzureRmSqlServer -ResourceGroupName $ResourceGroupName -ServerName $ServerName
    }
    catch {}
}

if(-not $SqlServer)
{
    # Provision new SQL Database Server
    Write-InfoLog "Creating SQL Server in location: $Location" (Get-ScriptName) (Get-ScriptLineNumber)
    $sqlCredentials = New-Object System.Management.Automation.PSCredential($AdminLogin, ($AdminPassword | ConvertTo-SecureString -AsPlainText -Force))
    $SqlServer = New-AzureRmSqlServer -ResourceGroupName $ResourceGroupName -Location $Location -ServerName $ServerName -SqlAdministratorCredentials $sqlCredentials

    Write-InfoLog ("Created SQL Server: {0} in location: {1}" -f $SqlServer.ServerName,$SqlServer.Location) (Get-ScriptName) (Get-ScriptLineNumber)

    #######################################################
    # 2. Azure SQL Server configuration --> authentication

    Write-InfoLog "Configuring SQL Server" (Get-ScriptName) (Get-ScriptLineNumber)
    # To allow connections to the database server you must create a rule that specifies 
    # a range of IP addresses from which connections are allowed. 
    $result = New-AzureRmSqlServerFirewallRule -ResourceGroupName $ResourceGroupName -ServerName $SqlServer.ServerName -FirewallRuleName "allowall" `
    -StartIpAddress "1.1.1.1" -EndIpAddress "255.255.255.255"
}

if(-not $SqlServer)
{
    Write-ErrorLog "Failed to create or get SQL Server information, please check error logs for more information." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Failed to create or get SQL Server information, please check error logs for more information."
}


#######################################################
# 3. Create Azure SQL Database if not exists
try
{
    $database = Get-AzureRmSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName
}
catch {}

if(-not $database)
{
    try
    {
        Write-InfoLog "Creating SQL Database" (Get-ScriptName) (Get-ScriptLineNumber)
        $database = New-AzureRmSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName
    }
    catch 
    {
        Write-ErrorLog "Failed to create or get SQL Database information, please check error logs for more information." (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}

if($SqlServer -and (-not [String]::IsNullOrWhiteSpace($SqlServer.ServerName)) -and $database)
{
    $finishTime = Get-Date
    $totalSeconds = ($finishTime - $startTime).TotalSeconds
    Write-SpecialLog ("SQL Server: $($SqlServer.ServerName) with Database: $DatabaseName successfully created " + `
        "in Resource Group: $ResourceGroupName at Location: $Location. Time: $totalSeconds secs") `
        (Get-ScriptName) (Get-ScriptLineNumber)
    return $SqlServer.ServerName
}
else
{
    Write-ErrorLog "Unable to return SQL Server information" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Unable to return SQL Server information"
}
