if(Get-Command Add-AzureAccount -errorAction SilentlyContinue)
{
    $True
}
else
{
    Write-Host "ERROR!!!"
    Write-Host "You need to run this scripts in Azure Powershell Prompt" -foregroundcolor red
    Write-Host "Please follow the guide to install Azure Powershell" -foregroundcolor red
    Write-Host "http://azure.microsoft.com/en-us/documentation/articles/install-configure-powershell/#Install" -foregroundcolor red
    $False
}
