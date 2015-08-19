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

if(Test-Path env:JAVA_HOME)
{
    Write-InfoLog "Found JAVA_HOME environment variable with value: '$env:JAVA_HOME'" (Get-ScriptName) (Get-ScriptLineNumber)
    if (-not($env:PATH -like "*$env:JAVA_HOME\bin*"))
    {
        $env:PATH = "$env:JAVA_HOME\bin;$env:JAVA_HOME;$env:PATH"
    }
}
else
{
    Write-ErrorLog "JAVA_HOME not found, please ensure that Java is installed on your system and environment variable JAVA_HOME is set." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "JAVA_HOME not found, please ensure that Java is installed on your system and environment variable JAVA_HOME is set."
}

& "$scriptDir\installMvnLib.ps1"

$buildList=@()
$buildErrorList=@()

#Build a list of all Java projects by searching for pom.xml and exclude any pom.xml that lies under any target directory
$javaProjects = gci -Recurse -Filter pom.xml | ? { -not ($_.FullName -like "*\target\*") }
Write-SpecialLog "Building Java Projects" (Get-ScriptName) (Get-ScriptLineNumber)
Write-SpecialLog "======================" (Get-ScriptName) (Get-ScriptLineNumber)
$javaProjects | % {
    Write-SpecialLog ("Building Java Project: " + $_.Directory) (Get-ScriptName) (Get-ScriptLineNumber)
    pushd $_.Directory
    $projectName=$_.FullName
    try
    {
        mvn clean package
        if($LASTEXITCODE -ne 0) { $buildErrorList += $projectName } else { $buildList += $projectName }; 
    }
    catch [System.Management.Automation.CommandNotFoundException]
    {
        $buildErrorList += $projectName
        Write-ErrorLog "An exception has occurred while building: $projectName"  (Get-ScriptName) (Get-ScriptLineNumber) $_
    }
    finally
    {
        popd
    }
}

Write-SpecialLog "Project building complete!`r`n" (Get-ScriptName) (Get-ScriptLineNumber)
Write-SpecialLog "Build Summary:" (Get-ScriptName) (Get-ScriptLineNumber)
Write-SpecialLog "======================" (Get-ScriptName) (Get-ScriptLineNumber)

if($buildErrorList.Count -ne 0)
{
    Write-ErrorLog "ERROR: One or more project failed to build:" (Get-ScriptName) (Get-ScriptLineNumber)
    $buildErrorList | % { Write-ErrorLog $_ (Get-ScriptName) (Get-ScriptLineNumber)}
    Write-SpecialLog "Projects built successfully:" (Get-ScriptName) (Get-ScriptLineNumber)
    $buildList | % { Write-SpecialLog $_  (Get-ScriptName) (Get-ScriptLineNumber) }
    throw "Some projects failed to build, please check build output for error information."
}
else
{
    Write-SpecialLog "SUCCESS: All projects built successfully!" (Get-ScriptName) (Get-ScriptLineNumber)
    $buildList | % { Write-SpecialLog $_  (Get-ScriptName) (Get-ScriptLineNumber) }
}