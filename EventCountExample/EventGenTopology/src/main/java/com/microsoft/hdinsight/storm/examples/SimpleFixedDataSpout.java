package com.microsoft.hdinsight.storm.examples;

import java.util.Map;

import backtype.storm.spout.SpoutOutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseRichSpout;
import backtype.storm.tuple.Fields;
import backtype.storm.tuple.Values;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Generate <eventCount> messages, each with size <eventSize>.
 * 
 * Note that if the spout crashed and restarted, it will lose count and generate another
 * <eventCount> messages.
 */
public class SimpleFixedDataSpout extends BaseRichSpout {
  private static final Logger logger = LoggerFactory.getLogger(SimpleFixedDataSpout.class);
  
  private int eventSize;
  private long eventCount;
  private long currCount;
  private SpoutOutputCollector collector;
  
  private String stringToSend;
  
  public SimpleFixedDataSpout(int eventSize, long eventCount) {
    this.eventSize = eventSize;
    this.eventCount = eventCount;
  }
  
  @Override
  public void nextTuple() {
    currCount++;
    
    if((eventCount > 0) && currCount > eventCount) {
      try {
        if(currCount % 10 == 0) {
          logger.info("Each bolt has been sent " + eventCount + " events by this spout task. It will not send any more events.");
        }
        Thread.sleep(1000);
      }
      catch(Exception e) {}
      return;
    }
    
    collector.emit(new Values(String.valueOf(currCount), stringToSend));
    if(currCount == eventCount) {
      logger.info("Finished sending events: " + currCount);
    }
  }

  @Override
  public void open(Map stormConf, TopologyContext context, SpoutOutputCollector collector) {
    stringToSend = getStringToSend(eventSize);
    this.collector = collector;
    currCount = 0;
    logger.info("Fixed data string that will be sent: " + stringToSend);
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
    declarer.declare(new Fields("key", "message"));
  }

  private String getStringToSend(int size) {
    String staticString = "abcdefghijklmnopqrstuvwxyz0123456789";
    
    StringBuilder sb = new StringBuilder();
    while (sb.length() < size) {
      sb.append(staticString);
    }
    
    return sb.toString().substring(0, size);
  }
}