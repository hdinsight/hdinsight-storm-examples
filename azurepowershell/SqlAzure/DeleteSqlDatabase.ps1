[CmdletBinding(PositionalBinding=$true)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9]*$")]
    [String]$ServerName,                     # required    needs to be alphanumeric
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9A-Z_-]*$")]
    [String]$DatabaseName,                   # required
    [Parameter(Mandatory = $true)]
    [String]$AdminLogin,                     # required
    [Parameter(Mandatory = $true)]
    [String]$AdminPassword                   # required
    )

# Create connection to server using SQL Authentication
$ServerCreds = New-Object System.Management.Automation.PSCredential($AdminLogin,($AdminPassword `
    | ConvertTo-SecureString -AsPlainText -Force))

$Context = New-AzureSqlDatabaseServerContext -ServerName $ServerName -Credential $ServerCreds


Write-Host "Deleting SQL Database [$DatabaseName]"
Remove-AzureSqlDatabase $Context -DatabaseName $DatabaseName -Force

Write-Host "Deleting SQL Server [$ServerName]"
Remove-AzureSqlDatabaseServer $ServerName -Force

Write-Host "Successfully deleted database and server"