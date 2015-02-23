[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-zA-Z0-9-]*$")]
    [String]$clusterName,                           # required    needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [String]$clusterUsername,                       # required
    [Parameter(Mandatory = $true)]
    [String]$clusterPassword,                       # required
    [Parameter(Mandatory = $true)]
    [String]$jarPath,                               # required    path of the jar in WASB to submit
    [Parameter(Mandatory = $true)]
    [String]$className,                             # required
    [String]$additionalParam,                       # required    at least include the topology name
    [String]$clusterDnsSuffix="azurehdinsight.net"  # optional
    )
#example:
#.\SubmitStormTopology.ps1 shanyuautostorm admin HdInsight123! /Storm/SubmittedJars/storm-starter.jar storm.starter.ExclamationTopology hahaex

$url = "https://{0}.{1}{2}" -f $clusterName,$clusterDnsSuffix,"/StormDashboard/SubmitWasbJar"
$body = @{
    FilePath = $jarPath;
    ClassName = $className;
    AdditionalParameters = $additionalParam;
}
$securePwd = ConvertTo-SecureString $clusterPassword -AsPlainText -Force
$creds = New-Object System.Management.Automation.PSCredential($clusterUsername, $securePwd)

Write-Host "Sending POST to [$url]"
Invoke-RestMethod -Uri $url -Method "Post" -Body $body -Credential $creds

Write-Host "Successfully submitted topology"
