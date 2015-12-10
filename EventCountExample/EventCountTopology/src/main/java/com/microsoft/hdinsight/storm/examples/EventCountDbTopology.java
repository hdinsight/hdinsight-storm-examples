package com.microsoft.hdinsight.storm.examples;

import java.io.FileReader;
import java.util.Properties;
import java.util.UUID;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import backtype.storm.Config;
import backtype.storm.StormSubmitter;
import backtype.storm.generated.StormTopology;
import backtype.storm.spout.SchemeAsMultiScheme;
import backtype.storm.topology.TopologyBuilder;
import storm.kafka.BrokerHosts;
import storm.kafka.KafkaSpout;
import storm.kafka.SpoutConfig;
import storm.kafka.StringScheme;
import storm.kafka.ZkHosts;

import com.microsoft.eventhubs.spout.EventHubSpout;
import com.microsoft.eventhubs.spout.EventHubSpoutConfig;

public class EventCountDbTopology {
  private static final Logger logger = LoggerFactory.getLogger(EventCountDbTopology.class);
  
  protected Boolean useEventHubs = false; //by default uses Kafka otherwise
  
  protected EventHubSpoutConfig ehSpoutConfig;

  protected String kafkaBrokers;
  protected String kafkaZookeepers;
  protected String kafkaTopic;
  
  protected int partitionCount;
  protected long eventCountPerPartition;
  
  protected String sqlConnectionStr;
  
  int tickTupleFreqSecs = 5;
  Boolean enableAck = false;
  
  protected void readConfig(Properties properties) throws Exception {
    if(useEventHubs) {
      String ehUsername = properties.getProperty("eventhubs.username");
      logger.info("eventhubs.username = " + ehUsername);
      
      String ehPassword = properties.getProperty("eventhubs.password");
      
      String ehNamespace = properties.getProperty("eventhubs.namespace");
      logger.info("eventhubs.namespace = " + ehNamespace);
  
      String ehEntityPath = properties.getProperty("eventhubs.entitypath");
      logger.info("eventhubs.entitypath = " + ehEntityPath);
      
      String ehTargetFqnAddress = properties.getProperty("eventhubs.targetfqnaddress", "servicebus.windows.net");
      logger.info("eventhubs.targetfqnaddress = " + ehTargetFqnAddress);
      
      int ehCheckpointInterval = Integer.parseInt(properties.getProperty("eventhubs.checkpoint.interval", "10"));
      
      int ehReceiverCredits = Integer.parseInt(properties.getProperty("eventhubs.receiver.credits", "1024"));
      
      String ehMaxPendingMsgsPerPartitionStr = properties.getProperty("eventhubs.max.pending.messages.per.partition", "1024");
      int ehMaxPendingMsgsPerPartition = Integer.parseInt(ehMaxPendingMsgsPerPartitionStr);
      
      String ehEnqueueTimeDiffStr = properties.getProperty("eventhub.receiver.filter.timediff", "0");
      int ehEnqueueTimeDiff = Integer.parseInt(ehEnqueueTimeDiffStr);
      long ehEnqueueTimeFilter = 0;
      if(ehEnqueueTimeDiff != 0) {
        ehEnqueueTimeFilter = System.currentTimeMillis() - ehEnqueueTimeDiff*1000;
      }
      
      String stormZkEndpointAddress = properties.getProperty("storm.zookeepers");
      ehSpoutConfig = new EventHubSpoutConfig(ehUsername, ehPassword,
          ehNamespace, ehEntityPath, partitionCount, stormZkEndpointAddress,
          ehCheckpointInterval, ehReceiverCredits, ehMaxPendingMsgsPerPartition, ehEnqueueTimeFilter);
  
      if(ehTargetFqnAddress != null) {
        ehSpoutConfig.setTargetAddress(ehTargetFqnAddress);      
      }
    } else {
      kafkaBrokers = properties.getProperty("kafka.brokers");
      logger.info("kafka.brokers = " + kafkaBrokers);
      
      kafkaZookeepers = properties.getProperty("kafka.zookeepers");
      logger.info("kafka.zookeepers = " + kafkaZookeepers);
      
      kafkaTopic = properties.getProperty("kafka.topic");
      logger.info("kafka.topic = " + kafkaTopic);
    }
    partitionCount = Integer.parseInt(properties.getProperty("partition.count"));
    logger.info("partition.count = " + partitionCount);
    
    //read sqldb configurations
    sqlConnectionStr = properties.getProperty("sqldb.connection.str");
    
    tickTupleFreqSecs = Integer.parseInt(properties.getProperty("tick.tuple.freq.secs", "5"));
    enableAck = Boolean.parseBoolean(properties.getProperty("topology.ack.enabled"));
  }
  
  protected EventHubSpout createEventHubSpout() {
    EventHubSpout eventHubSpout = new EventHubSpout(ehSpoutConfig);
    return eventHubSpout;
  }
  
  protected StormTopology buildTopology() {
    TopologyBuilder topologyBuilder = new TopologyBuilder();

   
    if(useEventHubs) {
      topologyBuilder.setSpout(EventHubSpout.class.getSimpleName(), createEventHubSpout(), ehSpoutConfig.getPartitionCount())
        .setNumTasks(ehSpoutConfig.getPartitionCount());
    }
    else {
      BrokerHosts kafkaHosts = new ZkHosts(kafkaZookeepers);
      SpoutConfig kafkaSpoutConfig = new SpoutConfig(kafkaHosts, kafkaTopic, "/" + kafkaTopic, UUID.randomUUID().toString());
      kafkaSpoutConfig.scheme = new SchemeAsMultiScheme(new StringScheme());
      kafkaSpoutConfig.forceFromStart = true;
      KafkaSpout kafkaSpout = new KafkaSpout(kafkaSpoutConfig);
      
      topologyBuilder.setSpout(KafkaSpout.class.getSimpleName(), kafkaSpout, partitionCount)
      .setNumTasks(partitionCount);
    }
      
    topologyBuilder.setBolt(PartialCountBolt.class.getSimpleName(), new PartialCountBolt(enableAck, tickTupleFreqSecs), partitionCount)
      .localOrShuffleGrouping(
          (useEventHubs ? EventHubSpout.class.getSimpleName() : KafkaSpout.class.getSimpleName()))
      .setNumTasks(partitionCount);
    
    String tableName = "EventCount" + (useEventHubs ? "EventHubs" : "Kafka");
    
    topologyBuilder.setBolt(DBGlobalCountBolt.class.getSimpleName(), 
        new DBGlobalCountBolt(enableAck, sqlConnectionStr, tableName, tickTupleFreqSecs), 1)
      .globalGrouping(PartialCountBolt.class.getSimpleName()).setNumTasks(1);
    return topologyBuilder.createTopology();
  }
  
  protected void submitTopology(String[] args, StormTopology topology) throws Exception {
    Config config = new Config();
    config.setDebug(false);

    if (args != null && args.length > 0) {
      //set the number of workers to be the same as partition number.
      //the idea is to have a spout and a partial count bolt co-exist in one
      //worker to avoid shuffling messages across workers in storm cluster.
      config.setNumWorkers(partitionCount);
      if(enableAck) {
        config.setNumAckers(partitionCount);
      } else {
        config.setNumAckers(0);
      }
      
      config.put(Config.WORKER_CHILDOPTS, "-Xmx1g");
      config.setMaxSpoutPending(1000000);
      StormSubmitter.submitTopology(args[0], config, topology);
    }
  }
  
  protected void runScenario(String[] args) throws Exception{
    Properties properties = new Properties();
    if(args.length > 1) {
      logger.info("Scenario = " + args[1]);
      if(args[1].equalsIgnoreCase("eventhubs")) {
        useEventHubs = true;
      }
      logger.info("useEventHubs = " + useEventHubs);
    }
    
    if(args.length > 2) {
      logger.info("Loading properties from file = " + args[2]);
      properties.load(new FileReader(args[2]));
    } else {
      logger.info("Loading properties from resources");
      properties.load(this.getClass().getClassLoader().getResourceAsStream(
          "myconfig.properties"));
    }
    
    readConfig(properties);
    
    if(useEventHubs && args.length > 0) {
      //set topology name so that the Trident topology can use it as a stream name.
      ehSpoutConfig.setTopologyName(args[0]);
    }
    
    StormTopology topology = buildTopology();
    submitTopology(args, topology);
  }
  
  public static void main(String[] args) throws Exception {
    if(args.length == 0) {
      throw new Exception("Please specify at least one argument. Usage: [topology_name] [kafka or eventhubs] [properties_file]");
    }
    
    EventCountDbTopology scenario = new EventCountDbTopology();
    scenario.runScenario(args);
  }
}
