function GetRandomAlphaNumeric(
    [Int]$length
)
{
    $set = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray()
    $i = 1
    while($i -le $length)
    {
        $result += $set | Get-Random
        $i = $i + 1
    }
    $result
}

$config = @{
    RANDOM_EH_PASSWORD = GetRandomAlphaNumeric(44)
    RANDOM_CLUSTER_NAME = GetRandomAlphaNumeric(6)
    RANDOM_CLUSTER_PASSWORD = GetRandomAlphaNumeric(10)
    RANDOM_SQL_PASSWORD = GetRandomAlphaNumeric(10)
}

.\ReplaceStringInFile.ps1 ".\configurations.properties.template" ".\configurations.properties" $config