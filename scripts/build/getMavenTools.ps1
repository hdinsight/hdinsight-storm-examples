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
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

if(Test-Path env:M2_HOME)
{
    Write-InfoLog "Found M2_HOME environment variable with value: '$env:M2_HOME'" (Get-ScriptName) (Get-ScriptLineNumber)
}
else
{
    Write-ErrorLog "M2_HOME not found. Downloading Maven tools." (Get-ScriptName) (Get-ScriptLineNumber)
    $toolsDir = "$scriptDir\..\..\tools"
    $mavenDir = Join-Path $toolsDir "maven"
    $mavenVersion = "3.2.5"
    $mavenSource = "http://www.us.apache.org/dist/maven/maven-3/$mavenVersion/binaries/apache-maven-$mavenVersion-bin.zip"
    $mavenDownload = Join-Path $toolsDir "maven-bin.zip"
    Write-InfoLog ("Downloading Maven from " + $mavenSource + " to " + $mavenDownload) (Get-ScriptName) (Get-ScriptLineNumber)
    $assembly = Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (!(Test-Path -Path $mavenDir )) {
        $download = (new-object System.Net.WebClient).DownloadFile(
          $mavenSource,
          $mavenDownload
        )
        $extract = [System.IO.Compression.ZipFile]::ExtractToDirectory($mavenDownload, $mavenDir)
    }
    $env:M2_HOME = Join-Path $mavenDir "apache-maven-$mavenVersion"
}

if (-not($env:PATH -like "*$env:M2_HOME\bin*"))
{
    $env:PATH = "$env:M2_HOME\bin;$env:PATH"
}
