Remove-Item EventGenTopology\target -Force -Recurse
Remove-Item EventGenTopology\src\main\resources\myconfig.properties
Remove-Item EventGenTopology\submit_topology.ps1

Remove-Item EventCountTopology\target -Force -Recurse
Remove-Item EventCountTopology\src\main\resources\myconfig.properties
Remove-Item EventCountTopology\submit_topology.ps1

Remove-Item result.html