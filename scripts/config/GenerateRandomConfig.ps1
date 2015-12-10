[CmdletBinding()]
param(
    [parameter(Mandatory=$true)]
    [string]$ConfigFile,
    [String]$ResourcePrefix
)

###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit -9999
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

function GetRandomPassword(
    [ValidateRange(8,128)] 
    [Int]$length
)
{
    $upperAlphabets = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()
    $lowerAlphabets = "abcdefghijklmnopqrstuvwxyz".ToCharArray()
    $numbers = "0123456789".ToCharArray()
    $specialCharacters = "!@#$%^*()".ToCharArray()

    $allCharacters = $upperAlphabets + $lowerAlphabets + $numbers + $specialCharacters

    #Let's pick one of each character sets
    $result += $upperAlphabets | Get-Random
    $result += $lowerAlphabets | Get-Random
    $result += $numbers | Get-Random
    $result += $specialCharacters | Get-Random

    $i = 4

    #Fill the rest with random characters
    while($i -lt $length)
    {
        $result += $allCharacters | Get-Random
        $i = $i + 1
    }
    $result
}

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

if([String]::IsNullOrWhiteSpace($ResourcePrefix))
{
    $ResourcePrefix = "hdistormex"
}

$resourceName = $ResourcePrefix + [System.DateTime]::Now.ToString("yyyyMMddHHmm")

if($resourceName.Length -gt 24)
{
    $resourceName = $resourceName.Substring(0, 24)
}

$resourceGroupName = $ResourcePrefix + "-group-" + [System.DateTime]::Now.ToString("yyyyMMddHHmm")
if($resourceGroupName.Length -gt 32)
{
    $resourceGroupName = $resourceGroupName.Substring(0, 32)
}

$config = @{
    RANDOM_RESOURCE_GROUP = $resourceGroupName
    RANDOM_RESOURCE_NAME = $resourceName
    RANDOM_SQL_PASSWORD = GetRandomPassword(12)
    RANDOM_CLUSTER_PASSWORD = GetRandomPassword(12)
}

$runConfigDir = Split-Path $ConfigFile
mkdir $runConfigDir -ErrorAction SilentlyContinue

Write-InfoLog "Generating run configurations at $configFile" (Get-ScriptName) (Get-ScriptLineNumber)

if(-not (Test-Path $ConfigFile))
{
    Write-InfoLog "Creating a new run configuration at $configFile" (Get-ScriptName) (Get-ScriptLineNumber)
    &$scriptDir\ReplaceStringInFile.ps1 "$scriptDir\configurations.properties.template" $ConfigFile $config
}
else
{
    Write-InfoLog "An existing run configuration was found at $configFile, just updating newer entries." (Get-ScriptName) (Get-ScriptLineNumber)
    &$scriptDir\ReplaceStringInFile.ps1 $ConfigFile $ConfigFile $config
}