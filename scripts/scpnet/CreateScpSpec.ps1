[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [String]$TopologyAssembly,
    [Parameter(Mandatory = $true)]
    [string]$SpecFile,
    [string]$FullyQualifiedClassName
)

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

$scpC = & "$scriptDir\GetScpTools.ps1"

Write-InfoLog "Creating Scp spec for $TopologyAssembly at $SpecFile. ClassName (if any): $FullyQualifiedClassName" (Get-ScriptName) (Get-ScriptLineNumber)
& "$scpC" generate-spec -assembly $TopologyAssembly -spec $SpecFile -class $FullyQualifiedClassName

if(($LASTEXITCODE -ne 0) -or (-not (Test-Path $SpecFile)))
{
    Write-ErrorLog "ERROR: Failed to generate spec file. Please check error logs for more information." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "ERROR: Failed to generate spec file. Please check error logs for more information."
}
else
{
    Write-SpecialLog "Created Spec file at $SpecFile for $TopologyAssembly. ClassName (if any): $FullyQualifiedClassName" (Get-ScriptName) (Get-ScriptLineNumber)
}