<# 
.SYNOPSIS
  Install Kafka to HDInsight cluster.
   
.DESCRIPTION
  This installs Apache Kafka on HDInsight cluster. 
 
.EXAMPLE
  .\kafka-installer-v03.ps1 -KafkaBinaryZipLocation https://hdiconfigactions.blob.core.windows.net/kafkaconfigactionv01/kafka_2.11-0.8.2.1.zip -KafkaHomeName kafka_2.11-0.8.2.1
#>

param (
    # The binary location for Kafka in zip format. 
    [Parameter(Mandatory=$true)]
    [String]$KafkaBinaryZipLocation, 
 
    # The name of the folder for Kafka root.
    [Parameter(Mandatory=$true)]
    [String]$KafkaHomeName,
    
    # Alternative Unzip exe
    [Parameter(Mandatory=$false)]
    [String]$UnzipExeLocation, 
    
    # Additional Admin user that can remote into any node
    [Parameter(Mandatory=$false)]
    [String]$RemoteAdminUsername,
    
    [Parameter(Mandatory=$false)]
    [String]$RemoteAdminPassword
    )

# Creating Admin Account Functions
function Find-User ($username)
{
    $computer = [ADSI]("WinNT://$ENV:COMPUTERNAME,computer")
    $users = $computer.psbase.children | where { $_.psbase.schemaclassname -eq "User" }
    foreach( $user in $users )
    {
        if( $user.Name -eq $username ) {
            return $true
        }
    }
    return $false
}

function CreateAdminAccount(
  [String]
  [Parameter( Position=0, Mandatory=$true )]
  $username,
  [String]
  [Parameter( Position=1, Mandatory=$true )]
  $password
)
{
  if (!(Find-User $username)) {
    try {
      $computer = [ADSI]("WinNT://$ENV:COMPUTERNAME,computer")
      $user = $computer.Create("User", $username)
      $user.SetPassword($password)
      $user.SetInfo()
      $cmd = "net.exe localgroup administrators ""$ENV:COMPUTERNAME\$username"" /add"
      Invoke-CmdChk $cmd
    } catch [Exception] {
      return $false
    }
  }
  else
  {
    return $false
  }
  return $true
}

function AddToRdpGroup(
  [String]
  [Parameter( Position=0, Mandatory=$true )]
  $RemoteAdminUsername
)
{
  $rdpgroup = 'Remote Desktop Users'
  try {
    $cmd = "net.exe localgroup ""$rdpgroup"" ""$ENV:COMPUTERNAME\$RemoteAdminUsername"" /add"
    Invoke-CmdChk $cmd
  } catch [Exception] {
    return $false
  }
  return $true
}

function Invoke-Cmd ($command)
{
    Write-Output $command
    $out = cmd.exe /C "$command" 2>&1
    Write-Output $out
    return $out
}

function Invoke-CmdChk ($command)
{
  Write-Output $command
  $out = cmd.exe /C "$command" 2>&1
  Write-Output $out
  if (-not ($LastExitCode  -eq 0)) {
      throw "Command `"$out`" failed with exit code $LastExitCode "
  }
  return $out
}

function Update-ConfigProperty ($file, $name, $value)
{
  Write-Output ("Updating property: {0} with value: {1}" -f $name, $value)
  (Get-Content $file) | ForEach-Object { $_ -replace "$name=.+", "$name=$value" } |  Set-Content $file;
}

# MAIN

$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$webclient = New-Object System.Net.WebClient;

# Download config action module from a well-known directory.
$CONFIGACTIONURI = "https://hdiconfigactions.blob.core.windows.net/configactionmodulev03/HDInsightUtilities-v03.psm1";
$CONFIGACTIONMODULE = Join-Path "$env:HADOOP_HOME" "HDInsightUtilities.psm1";
Write-Output "Downloading HDInsightUtilities"
$webclient.DownloadFile($CONFIGACTIONURI, $CONFIGACTIONMODULE);

# (TIP) Import config action helper method module to make writing config action easy.
if (Test-Path ($CONFIGACTIONMODULE)) {
    Write-Output "Importing module HDInsightUtilities"
    Import-Module $CONFIGACTIONMODULE | Write-Output
}
else
{
    Write-Output "[ERROR] Failed to load HDInsightUtilities module, exiting ...";
    exit;
}

try {
    winrm set winrm/config/service/auth '@{Kerberos="true"}'
    Write-HDILog "Enabling PSRemoting"
    Enable-PSRemoting -Force
}
catch {
    Write-HDILog "[WARN] Could not enable PSRemoting"
}

if($RemoteAdminUsername -and $RemoteAdminPassword) {
    Write-HDILog "Creating Admin User: $RemoteAdminUsername";
    $create = CreateAdminAccount $RemoteAdminUsername $RemoteAdminPassword
    if(-not $create) {
        Write-HDILog "Admin user: $RemoteAdminUsername created"
    }
    else
    {
        Write-HDILog "[WARN] Unable to create Admin user: $RemoteAdminUsername"
    }
    $addRdp = AddToRdpGroup $RemoteAdminUsername
    if(-not $addRdp) {
        Write-HDILog "Admin user: $RemoteAdminUsername added to Rdp Group"
    }
    else
    {
        Write-HDILog "[WARN] Unable to add Admin user: $RemoteAdminUsername to Rdp Group"
    }
}

# Install Kafka
# Figure out the broker ID for individual node
$kafkaHost = hostname;

# Install Kafka brokers only on workernodes
$isKafkaBrokerNode = -not(Test-IsHDIHeadNode -or $kafkaHost.StartsWith("headnode", [StringComparison]::OrdinalIgnoreCase))

# (TIP) Write-HDILog is the way to write to STDOUT and STDERR in HDInsight config action script.
Write-HDILog "KafkaBinaryZipLocation: $KafkaBinaryZipLocation"
Write-HDILog "KafkaHomeName: $KafkaHomeName"

Write-HDILog "Starting Kafka installation at: $(Get-Date) on node: $kafkaHost";
if(-not $isKafkaBrokerNode) {
    Write-HDILog "[IMPORTANT] Kafka broker service will not be configured or started on headnode.";
}

# Define input to the Kafka installation script.
$KafkaName = $KafkaHomeName;
$HdpInstallationDir = (Get-Item "$env:HADOOP_HOME").Parent.FullName;
$KafkaHome = Join-Path $HdpInstallationDir $KafkaName;
try {
    [System.Environment]::SetEnvironmentVariable("KAFKA_HOME", $KafkaHome, "Machine") #machine wide
}
catch {
    Write-HDILog "[ERROR] Unable to set machine level environment variable KAFKA_HOME"
}
[System.Environment]::SetEnvironmentVariable("KAFKA_HOME", $KafkaHome) #for the current process as machine wide only takes effect on new processes
Write-HDILog "KafkaHome: $env:KAFKA_HOME";

# Add logging jars to Kafka classpath
Copy-Item "$HdpInstallationDir\azureLogging\*" "$kafkaHome\libs\" -Recurse -ErrorAction SilentlyContinue

$KafkaServerStart = "$KafkaHome\bin\windows\kafka-server-start.bat"
# (TIP) Test whether the destination file already exists and this makes the script idempotent so it functions properly upon reboot and re-image.
if ($isKafkaBrokerNode -and (Test-Path $KafkaHome) -and (Test-Path $KafkaServerStart)) {
    Write-HDILog "Destination: $KafkaHome exists. Checking for Kafka service ...";
    try {
        $kafkaService = Get-Service Kafka;
        Write-HDILog $kafkaService;
        if($kafkaService.State -ne "Running") {
            Write-HDILog "Attempting to start Kafka service ...";
            Start-Service Kafka;
        }
        exit;
    }
    catch {
        Write-HDILog "Error locating Kafka service. Will attempt installation ...";
    }
}

# Create destination directory if not exists.
New-Item -ItemType Directory -Force -Path $KafkaHome;

# Download the zip file into local file system.
# (TIP) It is always good to download to user temporary location.
$intermediate = $env:temp + '\' + $KafkaName + [guid]::NewGuid() + '.zip';
Save-HDIFile -SrcUri $KafkaBinaryZipLocation -DestFile $intermediate;

if($UnzipExeLocation) {
    Write-HDILog "Downloading Unzip from: $UnzipExeLocation"
    $UnzipExe = Join-Path $HdpInstallationDir "unzip.exe";
    Save-HDIFile -SrcUri $UnzipExeLocation -DestFile $UnzipExe;

    # Unzip the file into final destination.
    Write-Output "Unzipping $intermediate to: $KafkaHome"
    & $UnzipExe -qq -o $intermediate -d $KafkaHome
}
else {
    # Unzip the file into final destination.
    Expand-HDIZippedFile -ZippedFile $intermediate -UnzipFolder $KafkaHome;
}

# Remove the intermediate file we created.
# (TIP) Please clean up temporary files when no longer needed.
Remove-Item $intermediate;

# Figure out zookeeper ensemble to connect to.
$kafkaDnsDomain = (Get-WmiObject Win32_NetworkAdapterconfiguration -filter 'IPEnabled=True').DNSDomain;
$zkHosts = "zookeeper0.${kafkaDnsDomain}:2181,zookeeper1.${kafkaDnsDomain}:2181,zookeeper2.${kafkaDnsDomain}:2181";

if($isKafkaBrokerNode) {
    # Take the number part of the hostname as broker ID.
    $brokerId = $kafkaHost.Replace("workernode", "");
    $brokerId = [int]$brokerId;
    $brokerList = "$kafkaHost.${kafkaDnsDomain}:9092";
    Update-ConfigProperty $KafkaHome\config\server.properties "broker.id" $brokerId
}
else {
    #For headnode just add workernode0 in the metadata.broker.list
    $brokerList = "workernode0.${kafkaDnsDomain}:9092";
}

Update-ConfigProperty $KafkaHome\config\server.properties "zookeeper.connect" $zkHosts
Update-ConfigProperty $KafkaHome\config\consumer.properties "zookeeper.connect" $zkHosts
Update-ConfigProperty $KafkaHome\config\producer.properties "metadata.broker.list" $brokerList

if($isKafkaBrokerNode) {
    # Generate and configure Kafka startup script.
    echo "cd $KafkaHome" | Out-File $KafkaHome\kafkastart.bat -Encoding ASCII;
    echo "$KafkaServerStart $KafkaHome\config\server.properties" | Out-File $KafkaHome\kafkastart.bat -Encoding ASCII -append;

    try {
        Invoke-HDICmdScript "$KafkaHome\nssm.exe install Kafka $KafkaHome\kafkastart.bat";
        # Start Kafka service.
        Write-HDILog "Attempting to start Kafka service on Node: $kafkaHost with BrokerId: $brokerId";
        Start-Service Kafka;
        Get-Service Kafka | Write-HDILog;
    }
    catch
    {
        Write-HDILog $_
        Get-Process | ? { $_.Name -eq "nssm" } | Stop-Process
        Remove-Item -Recurse -Force $KafkaHome
        Write-HDILog "[ERROR] Unable to start Kafka service"
        throw "Unable to start Kafka service"
    }
}
else {
    Write-HDILog "Skipping Kafka broker service configuration and start up."
}

try {
    Write-HDILog "Creating desktop short-cuts..."
    $wshShell = New-Object -comObject WScript.Shell
    $desktopFolderPath = $wshShell.SpecialFolders.Item("AllUsersDesktop")

    Write-HDILog "Creating $desktopFolderPath\Kafka Command Line.lnk"
    $shortcut = $wshShell.CreateShortcut("$desktopFolderPath\Kafka Command Line.lnk")
    $shortcut.TargetPath="cmd.exe"
    $shortcut.Arguments="/k pushd `"$ENV:KAFKA_HOME`""
    $shortcut.IconLocation="$ENV:WINDIR\system32\cmd.exe"
    $shortcut.WorkingDirectory="$ENV:KAFKA_HOME"
    $shortcut.Save()

    Write-HDILog "Creating $desktopFolderPath\Kafka Documentation.lnk"
    $shortcut = $wshShell.CreateShortcut("$desktopFolderPath\Kafka Documentation.lnk")
    $shortcut.TargetPath="http://kafka.apache.org/documentation.html"
    $shortcut.Save()

    Write-HDILog "Creating $desktopFolderPath\Kafka Quick Start.lnk"
    $shortcut = $wshShell.CreateShortcut("$desktopFolderPath\Kafka Quick Start.lnk")
    $shortcut.TargetPath="http://kafka.apache.org/documentation.html#quickstart"
    $shortcut.Save()
}
catch {
    Write-HDILog "[WARN] Encountered an issue while creating short-cuts."
    Write-HDILog $_
}

Write-HDILog "Done with Kafka installation at: $(Get-Date)";
Write-HDILog "[SUCCESS] Installed Kafka at: $KafkaHome";