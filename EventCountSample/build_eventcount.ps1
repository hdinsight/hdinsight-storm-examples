$ErrorActionPreference = "Stop"

.\create_azure_resources.ps1

Write-Host "Press any key to continue to build source code..."
cmd /c pause | out-null

.\build.ps1
