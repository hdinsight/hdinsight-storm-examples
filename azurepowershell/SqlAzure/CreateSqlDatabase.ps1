#returns SQL Database server name
[CmdletBinding(PositionalBinding=$true)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9A-Z_-]*$")]
    [String]$DatabaseName,                   # required
    [Parameter(Mandatory = $true)]
    [String]$AdminLogin,                     # required
    [Parameter(Mandatory = $true)]
    [String]$AdminPassword,                  # required
    [Parameter(Mandatory = $false)]
    [String]$Location = "West Europe"        # optional
    )


#######################################################
# 1. Create Azure SQL Database Server

# Provision new SQL Database Server
Write-Host "Creating SQL Server"
$SqlServer = New-AzureSqlDatabaseServer -AdministratorLogin $AdminLogin `
    -AdministratorLoginPassword $AdminPassword -Location $Location
Write-Host "Created SQL Server [$($SqlServer.ServerName)]"

#######################################################
# 2. Azure SQL Server configuration --> authentication

Write-Host "Configuring SQL Server"
# To allow connections to the database server you must create a rule that specifies 
# a range of IP addresses from which connections are allowed. 
$result = New-AzureSqlDatabaseServerFirewallRule -ServerName $SqlServer.ServerName -RuleName "allowall" `
-StartIpAddress 1.1.1.1 -EndIpAddress 255.255.255.255

# Create connection to server using SQL Authentication
$ServerCreds = New-Object System.Management.Automation.PSCredential($AdminLogin,($AdminPassword `
    | ConvertTo-SecureString -AsPlainText -Force))

$Context = New-AzureSqlDatabaseServerContext -ServerName $SqlServer.ServerName -Credential $ServerCreds

#######################################################
# 3. Create Azure SQL Database

Write-Host "Creating SQL Database"
$result = New-AzureSqlDatabase -DatabaseName $DatabaseName -ConnectionContext $Context

Write-Host "Successfully created SQL server [$($SqlServer.ServerName)] and database [$DatabaseName]"

return $SqlServer.ServerName




