package com.microsoft.hdinsight.storm.samples;

import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import backtype.storm.task.OutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseRichBolt;
import backtype.storm.tuple.Fields;
import backtype.storm.tuple.Tuple;
import backtype.storm.tuple.Values;

/**
 * Partially count number of messages from EventHubs
 */
public class PartialCountBolt extends BaseRichBolt {
  private static final long serialVersionUID = 1L;
  private static final Logger logger = LoggerFactory
      .getLogger(PartialCountBolt.class);
  private static final int PartialCountBatchSize = 1000; 
  
  private int partialCount;
  private OutputCollector collector;
  
  @Override
  public void prepare(Map stormConf, TopologyContext context, OutputCollector collector) {
    partialCount = 0;
    this.collector = collector;
  }
  
  @Override
  public void execute(Tuple tuple) {
    partialCount++;
    if(partialCount == PartialCountBatchSize) {
      collector.emit(new Values(PartialCountBatchSize));
      partialCount = 0;
    }
    collector.ack(tuple);
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
    declarer.declare(new Fields("partial_count"));
  }

}
