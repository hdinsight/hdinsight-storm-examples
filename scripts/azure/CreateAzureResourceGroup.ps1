[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-zA-Z0-9-]*$")]
    [ValidateLength(8,64)]
    [String]$ResourceGroupName,             # required    needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [String]$Location                       # required
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

try
{
    $ResourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName
}
catch 
{
    Write-WarnLog "Could not find Resource Group: $ResourceGroupName. Will attempt to create a new one in Location: $Location" (Get-ScriptName) (Get-ScriptLineNumber)
}

if($ResourceGroup -eq $null)
{
    $startTime = Get-Date
    
    try
    {
        $ResourceGroup = New-AzureRmResourceGroup -Name $ResourceGroupName -Location "$Location" -Force
    }
    catch
    {
        Write-ErrorLog "Failed to create Azure Resource Group: $ResourceGroupName at Location: $Location" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
    
    Write-InfoLog ("AzureResourceGroup Information:`r`n" + ($ResourceGroup | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)

    $finishTime = Get-Date
    $totalSeconds = ($finishTime - $startTime).TotalSeconds
    Write-SpecialLog "Resource Group successfully created at Location: $Location. Time: $totalSeconds secs" (Get-ScriptName) (Get-ScriptLineNumber)
}
