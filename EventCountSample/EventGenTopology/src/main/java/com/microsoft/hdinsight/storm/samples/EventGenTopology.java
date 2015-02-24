package com.microsoft.hdinsight.storm.samples;

import java.io.FileReader;
import java.util.Properties;

import backtype.storm.Config;
import backtype.storm.StormSubmitter;
import backtype.storm.generated.StormTopology;
import backtype.storm.topology.TopologyBuilder;

import com.microsoft.eventhubs.spout.EventHubSpout;
import com.microsoft.eventhubs.spout.EventHubSpoutConfig;

public class EventGenTopology {
  protected String ehConnectStr;
  protected String ehEntityPath;
  protected int partitionCount;
  protected int eventSize;
  protected long eventCount; 
  
  protected void readEHConfig(Properties properties) throws Exception {
    String username = properties.getProperty("eventhubs.username");
    String password = properties.getProperty("eventhubs.password");
    String namespace = properties.getProperty("eventhubs.namespace");
    String targetFqnAddress = properties.getProperty("eventhubs.targetfqnaddress");
    partitionCount = Integer.parseInt(properties.getProperty("eventhubs.partitions.count"));
    
    eventSize = Integer.parseInt(properties.getProperty("eventhubs.event.size"));
    eventCount = Long.parseLong(properties.getProperty("eventhubs.event.count.perpartition"));
    
    if(targetFqnAddress == null || targetFqnAddress.length()==0) {
      targetFqnAddress = "servicebus.windows.net";
    }
    
    ehConnectStr = EventHubSpoutConfig.buildConnectionString(
        username, password, namespace, targetFqnAddress);
    ehEntityPath = properties.getProperty("eventhubs.entitypath");
  }
  
  protected StormTopology buildTopology() {
    TopologyBuilder topologyBuilder = new TopologyBuilder();
    RandomDataSpout randomDataSpout = new RandomDataSpout(eventSize, eventCount);
    topologyBuilder.setSpout("RandomDataSpout", randomDataSpout, partitionCount)
      .setNumTasks(partitionCount);
    topologyBuilder.setBolt("EventHubsPartitionBolt", new EventHubsPartitionBolt(), partitionCount)
      .localOrShuffleGrouping("RandomDataSpout").setNumTasks(partitionCount);
    return topologyBuilder.createTopology();
  }
  
  protected void submitTopology(String[] args, StormTopology topology) throws Exception {
    Config config = new Config();
    config.setDebug(false);
    //disable acking because we do not care about exact-once at at-least-once
    config.setNumAckers(0);
    
    //pass parameters to EventHubsPartitionBolt
    config.put("ehConnectStr", ehConnectStr);
    config.put("ehEntityPath", ehEntityPath);

    if (args != null && args.length > 0) {
      config.setNumWorkers(partitionCount);
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
    
    StormTopology topology = buildTopology();
    submitTopology(args, topology);
  }
  
  public static void main(String[] args) throws Exception {
    EventGenTopology scenario = new EventGenTopology();
    scenario.runScenario(args);
  }
}
