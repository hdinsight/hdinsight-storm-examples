
$config = .\config\ReadConfig.ps1 ".\config\configurations.properties"
$config

.\config\ReplaceStringInFile.ps1 ".\EventGenTopology\myconfig.properties.template" ".\EventGenTopology\src\main\resources\myconfig.properties" $config
.\config\ReplaceStringInFile.ps1 ".\EventGenTopology\submit_topology.ps1.template" ".\EventGenTopology\submit_topology.ps1" $config

.\config\ReplaceStringInFile.ps1 ".\EventCountTopology\myconfig.properties.template" ".\EventCountTopology\src\main\resources\myconfig.properties" $config
.\config\ReplaceStringInFile.ps1 ".\EventCountTopology\submit_topology.ps1.template" ".\EventCountTopology\submit_topology.ps1" $config

.\config\ReplaceStringInFile.ps1 ".\result.html.template" ".\result.html" $config


