package com.microsoft.hdinsight.storm.examples;

import java.io.FileReader;
import java.util.Properties;

import backtype.storm.Config;
import backtype.storm.StormSubmitter;
import backtype.storm.generated.StormTopology;
import backtype.storm.topology.TopologyBuilder;

import com.microsoft.eventhubs.spout.EventHubSpout;
import com.microsoft.eventhubs.spout.EventHubSpoutConfig;

public class EventCountDbTopology {
  protected EventHubSpoutConfig spoutConfig;
  protected String sqlConnectionStr;
  
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
    
    //read sqldb configurations
    sqlConnectionStr = properties.getProperty("sqldb.connection.str");
  }
  
  protected EventHubSpout createEventHubSpout() {
    EventHubSpout eventHubSpout = new EventHubSpout(spoutConfig);
    return eventHubSpout;
  }
  
  protected StormTopology buildTopology(EventHubSpout eventHubSpout) {
    TopologyBuilder topologyBuilder = new TopologyBuilder();

    int partitionCount = spoutConfig.getPartitionCount();
    
    topologyBuilder.setSpout(EventHubSpout.class.getSimpleName(), eventHubSpout, spoutConfig.getPartitionCount())
      .setNumTasks(spoutConfig.getPartitionCount());
    topologyBuilder.setBolt(PartialCountBolt.class.getSimpleName(), new PartialCountBolt(), partitionCount)
      .localOrShuffleGrouping(EventHubSpout.class.getSimpleName()).setNumTasks(partitionCount);
    topologyBuilder.setBolt(DBGlobalCountBolt.class.getSimpleName(), new DBGlobalCountBolt(sqlConnectionStr), 1)
      .globalGrouping(PartialCountBolt.class.getSimpleName()).setNumTasks(1);
    return topologyBuilder.createTopology();
  }
  
  protected void submitTopology(String[] args, StormTopology topology) throws Exception {
    Config config = new Config();
    config.setDebug(false);
    config.setMaxSpoutPending(512);

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
      properties.load(EventCountDbTopology.class.getClassLoader().getResourceAsStream(
          "myconfig.properties"));
    }
    
    readEHConfig(properties);
    if(args.length > 0) {
      //set topology name so that the Trident topology can use it as a stream name.
      spoutConfig.setTopologyName(args[0]);
    }
    
    EventHubSpout eventHubSpout = createEventHubSpout();
    StormTopology topology = buildTopology(eventHubSpout);
    submitTopology(args, topology);
  }
  
  public static void main(String[] args) throws Exception {
    EventCountDbTopology scenario = new EventCountDbTopology();
    scenario.runScenario(args);
  }
}
