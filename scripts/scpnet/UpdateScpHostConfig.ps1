[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigFile,
    [Parameter(Mandatory = $true)]
    [hashtable]$updateConfig
)

$appConfig = New-Object XML
$appConfig.Load($ConfigFile)

foreach($appSetting in $appConfig.configuration.appSettings.add)
{
    if($updateConfig.ContainsKey($appSetting.key))
    {
        Write-InfoLog ("Updating " + $appSetting.key + " with new value") (Get-ScriptName) (Get-ScriptLineNumber)
        $appSetting.value = $updateConfig[$appSetting.key]
    }
}

$appConfig.Save($ConfigFile)