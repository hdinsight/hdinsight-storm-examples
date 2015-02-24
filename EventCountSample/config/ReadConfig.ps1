[CmdletBinding(PositionalBinding=$True)]
Param(
    [string]
    [parameter( Position=0, Mandatory=$true)]
    $fileName
    )

$config = @{}
$content = Get-Content $fileName
foreach($line in $content)
{
    if(-not $line.startsWith("#"))
    {
        $a = $line.split("=", 2)
        if($a.Length -eq 2)
        {
            $config.Add($a[0].trim(), $a[1].trim())
        }
    }
}
return $config
