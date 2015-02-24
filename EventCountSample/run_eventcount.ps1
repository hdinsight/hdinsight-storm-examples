$ErrorActionPreference = "Stop"

Write-Host "Starting Storm topology to generate events" -foregroundcolor yellow
pushd EventGenTopology
#submit topology with name gen001 to generate events
$url = .\submit_topology.ps1 "gen001"
popd

$config = .\config\ReadConfig.ps1 ".\config\configurations.properties"

Write-Host "About to start browser for Storm dashboard: " $url -foregroundcolor yellow 
Write-Host "Please type the following credentials to your browser window:"
Write-Host "    Username:"$config["HDINSIGHT_CLUSTER_USERNAME"]
Write-Host "    Password:"$config["HDINSIGHT_CLUSTER_PASSWORD"]
Write-Host "Press any key to continue..."
cmd /c pause | out-null
$result = (new-object -com wscript.shell).run($url,3)

Write-Host "When you see the topology finishes sending events to EventHubs"
Write-Host "If you don't know when it finishes, just wait for 10 minutes"
Write-Host "Kill the topology gen001 using Storm UI"
Write-Host "Then press any key to submit event count topology..."
cmd /c pause | out-null

Write-Host "Starting Storm topology to count events" -foregroundcolor yellow
pushd EventCountTopology
#submit topology with name ec001 to generate events
$url = .\submit_topology.ps1 "ec001"
popd

#open result.html to view the count
$url=".\result.html"
Invoke-Item $url
