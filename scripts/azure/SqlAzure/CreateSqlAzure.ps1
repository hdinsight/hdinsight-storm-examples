#returns SQL Database server name
[CmdletBinding(PositionalBinding=$true)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ServerName,                     # required - OKAY to pass empty string to create one
    [Parameter(Mandatory = $true)]
    [String]$DatabaseName,                   # required
    [Parameter(Mandatory = $true)]
    [String]$AdminLogin,                     # required
    [Parameter(Mandatory = $true)]
    [String]$AdminPassword,                  # required
    [Parameter(Mandatory = $false)]
    [String]$Location = "West Europe"        # optional
    )

#######################################################
# 1. Create Azure SQL Database Server if not exists

if(-not ([String]::IsNullOrWhiteSpace($ServerName)))
{
    #If a bad server name was passed, do not throw as we will create a new one
    try
    {
        $SqlServer = Get-AzureSqlDatabaseServer $ServerName
    }
    catch {}
}

if(-not $SqlServer)
{
    # Provision new SQL Database Server
    Write-InfoLog "Creating SQL Server" (Get-ScriptName) (Get-ScriptLineNumber)
    $SqlServer = New-AzureSqlDatabaseServer -AdministratorLogin $AdminLogin `
        -AdministratorLoginPassword $AdminPassword -Location $Location
    Write-InfoLog ("Created SQL Server: " + $SqlServer.ServerName) (Get-ScriptName) (Get-ScriptLineNumber)

    #######################################################
    # 2. Azure SQL Server configuration --> authentication

    Write-InfoLog "Configuring SQL Server" (Get-ScriptName) (Get-ScriptLineNumber)
    # To allow connections to the database server you must create a rule that specifies 
    # a range of IP addresses from which connections are allowed. 
    $result = New-AzureSqlDatabaseServerFirewallRule -ServerName $SqlServer.ServerName -RuleName "allowall" `
    -StartIpAddress 1.1.1.1 -EndIpAddress 255.255.255.255
}

if(-not $SqlServer)
{
    Write-ErrorLog "Failed to create or get SQL Server information, please check error logs for more information." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Failed to create or get SQL Server information, please check error logs for more information."
}

# Create connection to server using SQL Authentication
$ServerCreds = New-Object System.Management.Automation.PSCredential($AdminLogin,($AdminPassword | ConvertTo-SecureString -AsPlainText -Force))
$Context = New-AzureSqlDatabaseServerContext -ServerName $SqlServer.ServerName -Credential $ServerCreds

#######################################################
# 3. Create Azure SQL Database if not exists
try
{
    $database = Get-AzureSqlDatabase -DatabaseName $DatabaseName -ConnectionContext $Context
}
catch {}

if(-not $database)
{
    try
    {
        Write-InfoLog "Creating SQL Database" (Get-ScriptName) (Get-ScriptLineNumber)
        $database = New-AzureSqlDatabase -DatabaseName $DatabaseName -ConnectionContext $Context
    }
    catch 
    {
        Write-ErrorLog "Failed to create or get SQL Database information, please check error logs for more information." (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}

if($SqlServer -and (-not [String]::IsNullOrWhiteSpace($SqlServer.ServerName)) -and $database)
{
    Write-InfoLog "Successfully created SQL server [$($SqlServer.ServerName)] and database [$DatabaseName]" (Get-ScriptName) (Get-ScriptLineNumber)
    return $SqlServer.ServerName
}
else
{
    Write-ErrorLog "Unable to return SQL Server information" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Unable to return SQL Server information"
}