$ErrorActionPreference = "Stop"

.\create_resources_in_azure.ps1

Write-Host "Press any key to continue to build source code..."
cmd /c pause | out-null

.\build.ps1
