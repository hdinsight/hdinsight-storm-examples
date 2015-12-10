package com.microsoft.hdinsight.storm.examples;

import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.microsoft.eventhubs.client.EventHubClient;
import com.microsoft.eventhubs.client.EventHubSender;

import backtype.storm.task.OutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseRichBolt;
import backtype.storm.tuple.Fields;
import backtype.storm.tuple.Tuple;
import backtype.storm.tuple.Values;

/**
 * Bolt to send data to EventHubs entity
 */
public class EventHubsPartitionBolt extends BaseRichBolt {
  private static final long serialVersionUID = 1L;
  private static final Logger logger = LoggerFactory
      .getLogger(EventHubsPartitionBolt.class);

  private EventHubSender sender;
  private int myPartitionId;
  private long curCount;
  
  @Override
  public void prepare(Map stormConf, TopologyContext context, OutputCollector collector) {
    myPartitionId = context.getThisTaskIndex();
    
    //These configurations are mandatory, if not provided topology will die.
    String ehConnectStr = (String)stormConf.get("ehConnectStr");
    String ehEntityPath = (String)stormConf.get("ehEntityPath");
    
    try {
      EventHubClient client = EventHubClient.create(ehConnectStr, ehEntityPath);
      sender = client.createPartitionSender(myPartitionId+"");
      logger.info("Created sender for partition " + myPartitionId);
    }
    catch(Exception e) {
      logger.error("Failed to create EventHubSender for partition " + myPartitionId);
    }
    curCount = 0;
  }
  
  @Override
  public void execute(Tuple tuple) {
    try {
      sender.send(tuple.getStringByField("message"));
      curCount++;
      if(curCount % 10000 == 0) {
        logger.info("sent " + curCount + " messages for partition " + myPartitionId);
      }
    }
    catch(Exception e) {
      logger.error("Failed to send data for partition " + myPartitionId);
    }
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
  }

}
