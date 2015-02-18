$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath
$rootDir = Join-Path $scriptDir ".."
Write-Host "`r`nBuilding all projects under $rootDir"
Write-Host "`r`n"

pushd $rootDir
$nugetExePath = Join-Path $rootDir "tools\nuget"
$env:Path = $env:Path + ";" + $nugetExePath

$buildList=@()
$buildErrorList=@()

$csharpProjects = gci -Recurse -Filter *.sln
Write-Host "Building CSharp Projects"
Write-Host "========================"
$csharpProjects | % {
Write-Host ("Building CSharp Project: " + $_.Directory); 
pushd $_.Directory
$projectName=$_.FullName
try
{
    nuget.exe restore
    msbuild.exe /m /fl /flp:"Verbosity=Detailed" /clp:verbosity="Minimal;Summary" /t:"Clean;Build;Publish" /p:configuration="Debug" /p:platform="Any CPU" /p:GenerateManifests="true"; 
    if($LASTEXITCODE -ne 0) { $buildErrorList += $projectName } else { $buildList += $projectName }; 
}
catch [System.Management.Automation.CommandNotFoundException]
{
    $buildErrorList += $projectName
    Write-Host "An exception has occurred while building: $projectName" -ForegroundColor Red
    Write-Host $_  -ForegroundColor Red
}
popd
}

Write-Host "`r`nProject building complete!`r`n"
Write-Host "Build Summary:"
Write-Host "======================"

if($buildErrorList.Count -ne 0)
{
    Write-Host "`r`nERROR: One or more project failed to build:"
    $buildErrorList | % { Write-Host $_ }
    Write-Host "`r`nProjects built successfully:"
    $buildList | % { Write-Host $_ }
    exit -1;
}
else
{
    Write-Host "`r`nSUCCESS: All projects built successfully!"
    $buildList | % { Write-Host $_ }
}
popd
exit $LASTEXITCODE;