###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit /b -9999
}

$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

$libDir = Join-Path $scriptDir "..\..\lib"

if(Test-Path env:M2_HOME)
{
    Write-InfoLog "Found M2_HOME environment variable with value: '$env:M2_HOME'" (Get-ScriptName) (Get-ScriptLineNumber)
    if (-not($env:PATH -like "*$env:M2_HOME\bin*"))
    {
        $env:PATH = "$env:M2_HOME\bin;$env:PATH"
    }
}
else
{
    Write-WarnLog "M2_HOME not found. Please make sure that you either have M2_HOME set or include mvn in your path." (Get-ScriptName) (Get-ScriptLineNumber)
}

Write-SpecialLog "Pushing MVN libs" (Get-ScriptName) (Get-ScriptLineNumber)

# EventHubSpout jar
cmd /c "mvn -q install:install-file -Dfile=""$libDir\eventhubs\eventhubs-storm-spout-0.9-jar-with-dependencies.jar"" -DgroupId=com.microsoft.eventhubs -DartifactId=eventhubs-storm-spout -Dversion=0.9 -Dpackaging=jar"
if($LASTEXITCODE -ne 0)
{
    Write-ErrorLog "MVN push lib failure: EventHubSpout Jar" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "MVN push lib failure: EventHubSpout Jar"
}

# SQL JDBC
cmd /c "mvn -q install:install-file -Dfile=""$libDir\sql\sqljdbc4.jar"" -DgroupId=com.microsoft.sqlserver -DartifactId=sqljdbc4 -Dversion=4.0 -Dpackaging=jar"
if($LASTEXITCODE -ne 0)
{
    Write-ErrorLog "MVN push lib failure: SQL JDBC Jar" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "MVN push lib failure: SQL JDBC Jar"
}

cmd /c "mvn -q org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=""$libDir\hadoop\hadoop-azure-3.0.0-SNAPSHOT.jar"""
cmd /c "mvn -q org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=""$libDir\hadoop\hadoop-client-3.0.0-SNAPSHOT.jar"""
cmd /c "mvn -q org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=""$libDir\hadoop\hadoop-hdfs-3.0.0-SNAPSHOT.jar"""
if($LASTEXITCODE -ne 0)
{
    Write-ErrorLog "MVN push lib failure: Hadoop Jar" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "MVN push lib failure: Hadoop Jar"
}

# Push dependencies to local repo so our uber jar includes all dependencies
cmd /c "mvn -q org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=""$libDir\hadoop\hadoop-common-3.0.0-SNAPSHOT.jar"" -DpomFile=""$libDir\hadoop\hadoop-common-3.0.0-SNAPSHOT.pom"""
cmd /c "mvn -q org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=""$libDir\hadoop\hadoop-project-dist-3.0.0-SNAPSHOT.pom"" ""-DpomFile=""$libDir\hadoop\hadoop-project-dist-3.0.0-SNAPSHOT.pom"""
cmd /c "mvn -q org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=""$libDir\hadoop\hadoop-project-3.0.0-SNAPSHOT.pom"" -DpomFile=""$libDir\hadoop\hadoop-project-3.0.0-SNAPSHOT.pom"""
cmd /c "mvn -q org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=""$libDir\hadoop\hadoop-main-3.0.0-SNAPSHOT.pom"" -DpomFile=""$libDir\hadoop\hadoop-main-3.0.0-SNAPSHOT.pom"""
if($LASTEXITCODE -ne 0)
{
    Write-ErrorLog "MVN push lib failure: Hadoop Dependencies" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "MVN push lib failure:Hadoop Dependencies"
}