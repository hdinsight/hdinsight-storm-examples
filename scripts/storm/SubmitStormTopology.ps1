[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ClusterUrl,                           # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,                       # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword,                       # required
    [Parameter(Mandatory = $true)]
    [String]$JarPath,                               # required    path of the jar in WASB to submit
    [Parameter(Mandatory = $true)]
    [String]$ClassName,                             # required
    [String]$AdditionalParams                        # optional    at least include the topology name
    )

$clusterUri = new-object Uri($ClusterUrl)
$clusterSubmitJarUri = new-object Uri($clusterUri, "StormDashboard/SubmitWasbJar")

$body = @{
    FilePath = $JarPath;
    ClassName = $ClassName;
    AdditionalParameters = $AdditionalParams;
}
$securePwd = ConvertTo-SecureString $ClusterPassword -AsPlainText -Force
$creds = New-Object System.Management.Automation.PSCredential($ClusterUsername, $securePwd)

Write-InfoLog ("Sending POST request at: " + $clusterSubmitJarUri.AbsoluteUri) (Get-ScriptName) (Get-ScriptLineNumber)
try
{
    $response = Invoke-RestMethod -Uri $clusterSubmitJarUri.AbsoluteUri -Method "Post" -Body $body -Credential $creds
    if($?)
    {
        Write-SpecialLog "Successfully submitted topology: $ClassName" (Get-ScriptName) (Get-ScriptLineNumber)
        Write-InfoLog ("Response:`r`n" + ($response | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
    }
    else
    {
        $failure = $true
    }
}
catch
{
    $failure = $true
    Write-ErrorLog "Exception encountered while invoking the [POST] rest method at: $clusterSubmitJarUri" (Get-ScriptName) (Get-ScriptLineNumber) $_
}

if($failure)
{
    Write-ErrorLog "Topology submission encountered an error, please check logs for error information." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Topology submission encountered an error, please check logs for error information."
}