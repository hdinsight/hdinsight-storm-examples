package com.microsoft.hdinsight.storm.examples;

import java.io.FileReader;
import java.util.Properties;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import backtype.storm.Config;
import backtype.storm.StormSubmitter;
import backtype.storm.generated.StormTopology;
import backtype.storm.topology.TopologyBuilder;
import storm.kafka.bolt.KafkaBolt;
import storm.kafka.bolt.mapper.FieldNameBasedTupleToKafkaMapper;
import storm.kafka.bolt.selector.DefaultTopicSelector;

import com.microsoft.eventhubs.client.ConnectionStringBuilder;

public class EventGenTopology {
  private static final Logger logger = LoggerFactory.getLogger(EventGenTopology.class);
  
  protected Boolean useEventHubs = false; //by default uses Kafka otherwise
  
  protected String ehConnectStr;
  protected String ehEntityPath;

  protected String kafkaBrokers;
  protected String kafkaZookeepers;
  protected String kafkaTopic;
  
  protected int partitionCount;
  protected int eventSize;
  protected long eventCountPerPartition;
  
  protected Boolean runForever = false;
  
  protected void readConfig(Properties properties) throws Exception {
    if(useEventHubs) {
      String ehUsername = properties.getProperty("eventhubs.username");
      logger.info("eventhubs.username = " + ehUsername);
      
      String ehPassword = properties.getProperty("eventhubs.password");
      
      String ehNamespace = properties.getProperty("eventhubs.namespace");
      logger.info("eventhubs.namespace = " + ehNamespace);
      
      String ehTargetFqnAddress = properties.getProperty("eventhubs.targetfqnaddress", "servicebus.windows.net");
      logger.info("eventhubs.targetfqnaddress = " + ehTargetFqnAddress);
      
      ehConnectStr = new ConnectionStringBuilder(ehUsername, ehPassword,
          ehNamespace, ehTargetFqnAddress).getConnectionString();
      
      ehEntityPath = properties.getProperty("eventhubs.entitypath");
      logger.info("eventhubs.entitypath = " + ehEntityPath);
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
    
    eventSize = Integer.parseInt(properties.getProperty("event.size"));
    logger.info("event.size = " + eventSize);
    
    eventCountPerPartition = Long.parseLong(properties.getProperty("event.count.perpartition"));
    logger.info("event.count.perpartition = " + eventCountPerPartition);
    
    String runForeverStr = properties.getProperty("run.forever", "false");
    logger.info("run.forever = " + runForeverStr);
    runForever = Boolean.parseBoolean(runForeverStr);
    
    if(runForever)
    {
      eventCountPerPartition = -1;
    }
  }
  
  protected StormTopology buildTopology() {
    TopologyBuilder topologyBuilder = new TopologyBuilder();
    
    SimpleFixedDataSpout simpleFixedDataSpout = new SimpleFixedDataSpout(eventSize, eventCountPerPartition);
    topologyBuilder.setSpout(SimpleFixedDataSpout.class.getSimpleName(), simpleFixedDataSpout, partitionCount)
      .setNumTasks(partitionCount);
    
    if(useEventHubs)
    {
      topologyBuilder.setBolt(EventHubsPartitionBolt.class.getSimpleName(), new EventHubsPartitionBolt(), partitionCount)
        .shuffleGrouping(SimpleFixedDataSpout.class.getSimpleName()).setNumTasks(partitionCount);
    }
    else
    {
      KafkaBolt bolt = new KafkaBolt()
          .withTopicSelector(new DefaultTopicSelector(kafkaTopic))
          .withTupleToKafkaMapper(new FieldNameBasedTupleToKafkaMapper());
      topologyBuilder.setBolt(KafkaBolt.class.getSimpleName(), bolt, partitionCount)
        .shuffleGrouping(SimpleFixedDataSpout.class.getSimpleName()).setNumTasks(partitionCount);
    }
    return topologyBuilder.createTopology();
  }
  
  protected void submitTopology(String[] args, StormTopology topology) throws Exception {
    Config config = new Config();
    config.setDebug(false);
    //disable acking because we do not care about exactly-once or at-least-once semantics for this topology
    config.setNumAckers(0);
    
    if(useEventHubs) {
      //pass parameters to EventHubsPartitionBolt
      config.put("ehConnectStr", ehConnectStr);
      config.put("ehEntityPath", ehEntityPath);
    }
    else {
      //set producer properties.
      Properties props = new Properties();
      props.put("metadata.broker.list", kafkaBrokers);
      props.put("request.required.acks", "1");
      props.put("serializer.class", "kafka.serializer.StringEncoder");

      config.put(KafkaBolt.KAFKA_BROKER_PROPERTIES, props);
    }
    
    if (args != null && args.length > 0) {
      config.setNumWorkers(partitionCount);
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
    
    StormTopology topology = buildTopology();
    submitTopology(args, topology);
  }
  
  public static void main(String[] args) throws Exception {
    if(args == null || args.length == 0) {
      throw new Exception("Please specify at least one argument. Usage: [topology_name] [kafka or eventhubs] [properties_file]");
    }
    
    EventGenTopology scenario = new EventGenTopology();
    scenario.runScenario(args);
  }
}
