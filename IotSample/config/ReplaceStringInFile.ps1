[CmdletBinding(PositionalBinding=$True)]
Param(
    [string]
    [parameter( Position=0, Mandatory=$true)]
    $tempFileName,
    [string]
    [parameter( Position=1, Mandatory=$true)]
    $fileName,
    [hashtable]
    [parameter( Position=2, Mandatory=$true)]
    $config
    )

$content = Get-Content $tempFileName
foreach( $key in $config.Keys )
{
    $newVal = $config[$key]
    $content = $content -replace "{$key}", $newVal
}
Set-Content -path $fileName -value $content
