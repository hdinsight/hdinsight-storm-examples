<# 
.SYNOPSIS 
  Install Kafka to HDInsight cluster.
   
.DESCRIPTION 
  This installs Apache Kafka on HDInsight cluster. 
 
.EXAMPLE 
  .\kafka-installer-v01.ps1 -KafkaBinaryZipLocation https://hdiconfigactions.blob.core.windows.net/kafkaconfigactionv01/kafka_2.9.1-0.8.2-beta.zip -KafkaRootName kafka_2.9.1-0.8.2-beta
#> 
 
param ( 
    # The binary location for Kafka in zip format. 
    [Parameter()] 
    [String]$KafkaBinaryZipLocation, 
 
    # The name of the folder for Kafka root.
    [Parameter()] 
    [String]$KafkaRootName
    ) 

# Use default parameters in case they are not specified.
if (!$KafkaBinaryZipLocation) 
{ 
    $KafkaBinaryZipLocation = "https://hdiconfigactions.blob.core.windows.net/kafkaconfigactionv01/kafka_2.9.1-0.8.2-beta.zip"; 
}
if (!$KafkaRootName) 
{ 
    $KafkaRootName = "kafka_2.9.1-0.8.2-beta"; 
} 
    
# Download config action module from a well-known directory.
$CONFIGACTIONURI = "https://hdiconfigactions.blob.core.windows.net/configactionmodulev01/HDInsightUtilities-v01.psm1";
$CONFIGACTIONMODULE = "C:\apps\dist\HDInsightUtilities.psm1";
$webclient = New-Object System.Net.WebClient;
$webclient.DownloadFile($CONFIGACTIONURI, $CONFIGACTIONMODULE);

# (TIP) Import config action helper method module to make writing config action easy.
if (Test-Path ($CONFIGACTIONMODULE))
{ 
    Import-Module $CONFIGACTIONMODULE;
} 
else
{
    Write-Output "Failed to load HDInsightUtilities module, exiting ...";
    exit;
}

# (TIP) Write-HDILog is the way to write to STDOUT and STDERR in HDInsight config action script.
Write-HDILog "Starting Kafka installation at: $(Get-Date)";

# Define input to the Kafka installation script.
$Kafkaname = $KafkaRootName;
$Kafkainstallationdir = (Get-Item "$env:HADOOP_HOME").parent.FullName;
$Kafkaroot = "$Kafkainstallationdir\$Kafkaname";

# (TIP) Test whether the destination file already exists and this makes the script idempotent so it functions properly upon reboot and reimage.
if (Test-Path ($Kafkainstallationdir + '\' + $Kafkaname)) 
{
    Write-HDILog "Destination: $Kafkainstallationdir\$Kafkaname already exists, exiting ...";
    exit;
}

# Create destination directory if not exists.
New-Item -ItemType Directory -Force -Path $Kafkainstallationdir;

# Download the zip file into local file system.
# (TIP) It is always good to download to user temporary location.
$intermediate = $env:temp + '\' + $Kafkaname + [guid]::NewGuid() + '.zip';
Save-HDIFile -SrcUri $KafkaBinaryZipLocation -DestFile $intermediate;

# Unzip the file into final destination.
Expand-HDIZippedFile -ZippedFile $intermediate -UnzipFolder $Kafkainstallationdir;

# Remove the intermediate file we created.
# (TIP) Please clean up temporary files when no longer needed.
Remove-Item $intermediate;

# Figure out zookeeper ensemble to connect to.
$kafkaDnsDomain = (Get-WmiObject Win32_NetworkAdapterconfiguration -filter 'IPEnabled=True').DNSDomain;
$zkHosts = "zookeeper0.${kafkaDnsDomain}:2181,zookeeper1.${kafkaDnsDomain}:2181,zookeeper2.${kafkaDnsDomain}:2181";
(Get-Content $Kafkaroot\config\server.properties) | Foreach-Object { $_ -replace 'HDInsightZkConnection', $zkHosts  } |  Set-Content $Kafkaroot\config\server.properties;

# Figure out the broker ID for individual node
$kafkaHost = hostname;

# Take the number part of the hostname as broker ID.
$brokerId = $kafkaHost.Replace("headnode", "").Replace("workernode", "");

# Reserve broker ID 0 and 1 for HDInsight headnodes.
if ($kafkaHost.Contains("workernode")) 
{ 
    $brokerId = [int]$brokerId + 2;
}
(Get-Content $Kafkaroot\config\server.properties) | Foreach-Object { $_ -replace 'HDInsightBrokerId', $brokerId  } |  Set-Content $Kafkaroot\config\server.properties;


# Generate and configure Kafka startup script.
echo "cd $Kafkaroot" | Out-File $Kafkaroot\kafkastart.bat -Encoding ASCII;
echo ".\bin\windows\kafka-server-start.bat .\config\server.properties" | Out-File $Kafkaroot\kafkastart.bat -Encoding ASCII -append;
Invoke-HDICmdScript "$Kafkaroot\nssm.exe install Kafka $Kafkaroot\kafkastart.bat";

# Start Kafka service.
Start-Service Kafka;

Write-HDILog "Done with Kafka installation at: $(Get-Date)";
Write-HDILog "Installed Kafka at: $Kafkainstallationdir\$Kafkaname";