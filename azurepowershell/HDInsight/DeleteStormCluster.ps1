[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$ClusterName                     # required    needs to be alphanumeric or "-"
    )

Remove-AzureHDInsightCluster -Name $ClusterName