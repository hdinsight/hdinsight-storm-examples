package com.microsoft.hdinsight.storm.examples;

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
  
  static final long PartialCountBatchSize = 1000L;
  long partialCount = 0L;
  long totalCount = 0L;
  long finalCount = 3200000L;
  
  private OutputCollector collector;
  
  public PartialCountBolt(long finalCount) {
    this.finalCount = finalCount;
  }
  
  @Override
  public void prepare(Map stormConf, TopologyContext context, OutputCollector collector) {
    logger.info("finalCount: " + finalCount);
    partialCount = 0L;
    this.collector = collector;
  }
  
  @Override
  public void execute(Tuple tuple) {
    partialCount++;
    totalCount ++;

    //emit the partialCount when larger than PartialCountBatchSize
    //Specially handle the end of stream using finalCount so that this bolt flushes the count on last expected tuple
    if (partialCount >= PartialCountBatchSize || totalCount >= finalCount) {
      logger.info("emitting partialCount: " + partialCount + 
          ", totalCount: " + totalCount +
          ", finalCount: " + finalCount);
      collector.emit(new Values(partialCount));
      partialCount = 0L;
    }
    collector.ack(tuple);
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
    declarer.declare(new Fields("partialCount"));
  }
}