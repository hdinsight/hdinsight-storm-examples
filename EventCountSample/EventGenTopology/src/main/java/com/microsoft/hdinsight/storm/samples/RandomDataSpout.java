package com.microsoft.hdinsight.storm.samples;

import java.util.Map;
import java.util.Random;
import java.util.UUID;

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
public class RandomDataSpout extends BaseRichSpout {
  private static final Logger logger = LoggerFactory.getLogger(RandomDataSpout.class);
  
  private int eventSize;
  private long eventCount;
  private long curCount;
  private Random random;
  private SpoutOutputCollector collector;
  
  public RandomDataSpout(int eventSize, long eventCount) {
    this.eventSize = eventSize;
    this.eventCount = eventCount;
  }
  
  @Override
  public void nextTuple() {
    curCount++;
    
    if(curCount > eventCount) {
      try {
        Thread.sleep(1000);
      }
      catch(Exception e) {}
      return;
    }
    
    String str = getRandomString(random, eventSize);
    collector.emit(new Values(str));
    if(curCount == eventCount) {
      logger.info("Finished generating events " + curCount);
    }
  }

  @Override
  public void open(Map stormConf, TopologyContext context, SpoutOutputCollector collector) {
    random = new Random();
    this.collector = collector;
    curCount = 0;
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
    declarer.declare(new Fields("data"));
  }

  private String getRandomString(Random r, int size) {
    StringBuilder sb = new StringBuilder();
    for(int i=0; i<size; ++i) {
      sb.append((char)(r.nextInt(26)+97));
    }
    return sb.toString();
  }
}
