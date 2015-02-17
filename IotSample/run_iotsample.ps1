$ErrorActionPreference = "Stop"

Write-Host "Generating DocumentDB documents" -foregroundcolor yellow
pushd docdbgen
.\docdbgen.bat
popd

Write-Host "Generating EventHubs events" -foregroundcolor yellow
pushd eventgen
.\ioteventgen.bat
popd

Write-Host "Starting Storm topology" -foregroundcolor yellow
pushd iot
#submit topology with name iot001
$url = .\submit_topology.ps1 "iot001"
popd

$config = .\config\ReadConfig.ps1 ".\config\configurations.properties"

Write-Host "Storm topology will write to the following storage account:"
Write-Host "    Account:"$config["WASB_ACCOUNT_NAME"]
Write-Host "    Key:"$config["WASB_ACCOUNT_KEY"]

Write-Host "About to start browser for Storm dashboard: " $url -foregroundcolor yellow 
Write-Host "Please type the following credentials to your browser window:"
Write-Host "    Username:"$config["HDINSIGHT_CLUSTER_USERNAME"]
Write-Host "    Password:"$config["HDINSIGHT_CLUSTER_PASSWORD"]
Write-Host "Press any key to continue..."
cmd /c pause | out-null
$result = (new-object -com wscript.shell).run($url,3)

#Write-Host "About to start browser to show the files in WASB created by Storm topology" -foregroundcolor yellow 
#$url2 = "https://manage.windowsazure.com/microsoft.onmicrosoft.com#Workspaces/StorageExtension/StorageAccount/{0}/Container/{1}/Blobs" -f $config["WASB_ACCOUNT_NAME"],$config["WASB_CONTAINER"]
#Write-Host "URL: $url2"
#Write-Host "Press any key to continue..."
#cmd /c pause | out-null
#$result = (new-object -com wscript.shell).run($url2,3)