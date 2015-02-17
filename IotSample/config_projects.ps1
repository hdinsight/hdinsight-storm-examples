
$config = .\config\ReadConfig.ps1 ".\config\configurations.properties"
$config

.\config\ReplaceStringInFile.ps1 ".\docdbgen\docdb.config.template" ".\docdbgen\docdb.config" $config
.\config\ReplaceStringInFile.ps1 ".\eventgen\eventhubs.config.template" ".\eventgen\eventhubs.config" $config
.\config\ReplaceStringInFile.ps1 ".\iot\myconfig.properties.template" ".\iot\src\main\resources\myconfig.properties" $config
.\config\ReplaceStringInFile.ps1 ".\iot\core-site.xml.template" ".\iot\src\main\resources\core-site.xml" $config
.\config\ReplaceStringInFile.ps1 ".\iot\submit_topology.ps1.template" ".\iot\submit_topology.ps1" $config
