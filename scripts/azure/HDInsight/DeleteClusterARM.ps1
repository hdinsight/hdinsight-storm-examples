[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-zA-Z0-9-]*$")]
    [ValidateLength(8,64)]
    [String]$ResourceGroupName,             # required    needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [ValidateLength(8,64)]
    [String]$ClusterName                     # required    needs to be alphanumeric or "-"
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

Write-InfoLog "Deleting Azure HDInsight cluster: $ClusterName from Resource Group: $ResourceGroupName" (Get-ScriptName) (Get-ScriptLineNumber)

try
{
    $cluster = Get-AzureRmHDInsightCluster -ResourceGroupName $ResourceGroupName -ClusterName $ClusterName

    if ($cluster)
    {
        try
        {
            Remove-AzureRmHDInsightCluster -ResourceGroupName $ResourceGroupName -ClusterName $ClusterName
        }
        catch
        {
            Write-ErrorLog "Failed to delete HDInsight cluster: $ClusterName" (Get-ScriptName) (Get-ScriptLineNumber) $_
            throw
        }
    }
}
catch
{
    if ($_.Exception.Error.Code -eq "ResourceNotFound")
    {
        Write-InfoLog "Success! HDInsight cluster not found: $ClusterName" (Get-ScriptName) (Get-ScriptLineNumber) $_
    }
    elseif ($_.Exception.Error.Code -eq "ParentResourceNotFound")
    {
        Write-WarnLog "An unexpected error occured while deleting the HDInsight cluster: $ClusterName" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
    else
    {
        Write-ErrorLog "Could not get details for HDInsight cluster: $ClusterName" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}