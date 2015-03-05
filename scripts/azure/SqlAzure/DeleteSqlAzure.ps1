[CmdletBinding(PositionalBinding=$true)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ServerName                     # required    needs to be alphanumeric
    )

Write-InfoLog "Deleting SQL Server [$ServerName]" (Get-ScriptName) (Get-ScriptLineNumber)
Remove-AzureSqlDatabaseServer $ServerName -Force

Write-InfoLog "Successfully deleted server [$ServerName]" (Get-ScriptName) (Get-ScriptLineNumber)