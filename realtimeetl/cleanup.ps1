$title = "HDInsight Storm Examples Cleanup`r`n============================"
$message = "Enter 'yes' to clean up all the Azure resources created by HDInsight Storm and SCP.Net examples."

$yes = New-Object System.Management.Automation.Host.ChoiceDescription "&yes"
$no = New-Object System.Management.Automation.Host.ChoiceDescription "&no"

$options = [System.Management.Automation.Host.ChoiceDescription[]]($yes, $no)

$result = $host.ui.PromptForChoice($title, $message, $options, 0) 

switch ($result)
{
    0 { $runExamples = $true }
    1 { $runExamples = $false }
}

Write-Host "========================`r`n"
if($runExamples)
{
    $examplesBinDir="..\tools\HDInsight.Examples.CLI\bin\Debug"
    $examplesExe = Join-Path $examplesBinDir "HDInsight.Examples.CLI.exe"
    if(Test-Path $examplesExe)
    {
        pushd $examplesBinDir
        .\HDInsight.Examples.CLI.exe --m delete
        popd
    }
    else
    {
        throw "$examplesExe not found. Please ensure you have run the build.bat or build.ps1 to build the examples first."
    }
}

exit $LASTEXITCODE;