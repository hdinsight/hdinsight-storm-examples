Remove-Item docdbgen\target -Force -Recurse
Remove-Item docdbgen\docdb.config

Remove-Item eventgen\target -Force -Recurse
Remove-Item eventgen\eventhubs.config

Remove-Item iot\target -Force -Recurse
Remove-Item iot\src\main\resources\myconfig.properties
Remove-Item iot\src\main\resources\core-site.xml
Remove-Item iot\submit_topology.ps1

Remove-Item config\configurations.properties