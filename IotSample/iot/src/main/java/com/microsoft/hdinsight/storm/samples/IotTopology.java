package com.microsoft.hdinsight.storm.samples;

import java.io.FileReader;
import java.util.Properties;

import backtype.storm.Config;
import backtype.storm.StormSubmitter;
import backtype.storm.generated.StormTopology;
import backtype.storm.topology.TopologyBuilder;
import backtype.storm.tuple.Fields;

import com.microsoft.eventhubs.spout.EventHubSpout;
import com.microsoft.eventhubs.spout.EventHubSpoutConfig;

import org.apache.storm.hdfs.bolt.HdfsBolt;
import org.apache.storm.hdfs.bolt.format.DefaultFileNameFormat;
import org.apache.storm.hdfs.bolt.format.DelimitedRecordFormat;
import org.apache.storm.hdfs.bolt.format.FileNameFormat;
import org.apache.storm.hdfs.bolt.format.RecordFormat;
import org.apache.storm.hdfs.bolt.rotation.FileRotationPolicy;
import org.apache.storm.hdfs.bolt.rotation.FileSizeRotationPolicy;
import org.apache.storm.hdfs.bolt.rotation.FileSizeRotationPolicy.Units;
import org.apache.storm.hdfs.bolt.sync.CountSyncPolicy;
import org.apache.storm.hdfs.bolt.sync.SyncPolicy;

public class IotTopology
{
  protected EventHubSpoutConfig spoutConfig;
  protected String docdbHost;
  protected String docdbMasterKey;
  protected String docdbDatabase;
  protected String docdbCollection;
  
  protected void readEHConfig(Properties properties) throws Exception {
    String username = properties.getProperty("eventhubspout.username");
    String password = properties.getProperty("eventhubspout.password");
    String namespaceName = properties.getProperty("eventhubspout.namespace");
    String entityPath = properties.getProperty("eventhubspout.entitypath");
    String targetFqnAddress = properties.getProperty("eventhubspout.targetfqnaddress");
    String zkEndpointAddress = properties.getProperty("zookeeper.connectionstring");
    int partitionCount = Integer.parseInt(properties.getProperty("eventhubspout.partitions.count"));
    int checkpointIntervalInSeconds = Integer.parseInt(properties.getProperty("eventhubspout.checkpoint.interval"));
    int receiverCredits = Integer.parseInt(properties.getProperty("eventhub.receiver.credits"));
    String maxPendingMsgsPerPartitionStr = properties.getProperty("eventhubspout.max.pending.messages.per.partition");
    if(maxPendingMsgsPerPartitionStr == null) {
      maxPendingMsgsPerPartitionStr = "1024";
    }
    int maxPendingMsgsPerPartition = Integer.parseInt(maxPendingMsgsPerPartitionStr);
    String enqueueTimeDiffStr = properties.getProperty("eventhub.receiver.filter.timediff");
    if(enqueueTimeDiffStr == null) {
      enqueueTimeDiffStr = "0";
    }
    int enqueueTimeDiff = Integer.parseInt(enqueueTimeDiffStr);
    long enqueueTimeFilter = 0;
    if(enqueueTimeDiff != 0) {
      enqueueTimeFilter = System.currentTimeMillis() - enqueueTimeDiff*1000;
    }
    
    System.out.println("Eventhub spout config: ");
    System.out.println("  partition count: " + partitionCount);
    System.out.println("  checkpoint interval: " + checkpointIntervalInSeconds);
    System.out.println("  receiver credits: " + receiverCredits);
    spoutConfig = new EventHubSpoutConfig(username, password,
      namespaceName, entityPath, partitionCount, zkEndpointAddress,
      checkpointIntervalInSeconds, receiverCredits, maxPendingMsgsPerPartition, enqueueTimeFilter);

    if(targetFqnAddress != null)
    {
      spoutConfig.setTargetAddress(targetFqnAddress);
    }
    
    //read documentDb configurations
    docdbHost = properties.getProperty("documentdb.host");
    docdbMasterKey = properties.getProperty("documentdb.master.key");
    docdbDatabase = properties.getProperty("documentdb.database");
    docdbCollection = properties.getProperty("documentdb.collection");
  }
  
  protected EventHubSpout createEventHubSpout() {
    EventHubSpout eventHubSpout = new EventHubSpout(spoutConfig);
    return eventHubSpout;
  }
  
  protected StormTopology buildTopology(EventHubSpout eventHubSpout) {
    TopologyBuilder topologyBuilder = new TopologyBuilder();

    topologyBuilder.setSpout("EventHubsSpout", eventHubSpout, spoutConfig.getPartitionCount())
      .setNumTasks(spoutConfig.getPartitionCount());
    
    topologyBuilder.setBolt("TypeConversionBolt", new TypeConversionBolt(), spoutConfig.getPartitionCount())
      .localOrShuffleGrouping("EventHubsSpout").setNumTasks(spoutConfig.getPartitionCount());
    
    topologyBuilder.setBolt("DataReferenceBolt", 
        new DataReferenceBolt(docdbHost, docdbMasterKey, docdbDatabase, docdbCollection),
        spoutConfig.getPartitionCount())
      .localOrShuffleGrouping("TypeConversionBolt").setNumTasks(spoutConfig.getPartitionCount());
    
    //Create HdfsBolt to store data to Windows Azure Blob Storage
    SyncPolicy syncPolicy = new CountSyncPolicy(1000);
    FileRotationPolicy rotationPolicy = new FileSizeRotationPolicy(1.0f, Units.MB);

    RecordFormat recordFormat = new DelimitedRecordFormat().withFieldDelimiter(",");
    FileNameFormat fileNameFormat = new DefaultFileNameFormat().withPath("/iotsampledata/");
    HdfsBolt wasbBolt = new HdfsBolt()
      .withFsUrl("wasb:///")
      .withRecordFormat(recordFormat)
      .withFileNameFormat(fileNameFormat)
      .withRotationPolicy(rotationPolicy)
      .withSyncPolicy(syncPolicy);
    
    topologyBuilder.setBolt("WasbStoreBolt", wasbBolt, 10)
      .fieldsGrouping("DataReferenceBolt", new Fields("model")).setNumTasks(10);
    
    return topologyBuilder.createTopology();
  }
  
  protected void submitTopology(String[] args, StormTopology topology) throws Exception {
    Config config = new Config();
    config.setDebug(false);
    config.setMaxSpoutPending(64);

    if (args != null && args.length > 0) {
      //set the number of workers to be the same as partition number.
      //the idea is to have a spout and a partial count bolt co-exist in one
      //worker to avoid shuffling messages across workers in storm cluster.
      config.setNumWorkers(spoutConfig.getPartitionCount());
      StormSubmitter.submitTopology(args[0], config, topology);
    }
  }
  
  protected void runScenario(String[] args) throws Exception{
    Properties properties = new Properties();
    if(args.length > 1) {
      properties.load(new FileReader(args[1]));
    }
    else {
      properties.load(this.getClass().getClassLoader().getResourceAsStream(
          "myconfig.properties"));
    }
    
    readEHConfig(properties);
    if(args.length > 0) {
      spoutConfig.setTopologyName(args[0]);
    }
    
    EventHubSpout eventHubSpout = createEventHubSpout();
    StormTopology topology = buildTopology(eventHubSpout);
    submitTopology(args, topology);
  }
  
  public static void main(String[] args) throws Exception {
    IotTopology scenario = new IotTopology();
    scenario.runScenario(args);
  }
}
