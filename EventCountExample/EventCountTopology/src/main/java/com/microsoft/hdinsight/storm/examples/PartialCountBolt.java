package com.microsoft.hdinsight.storm.examples;

import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import backtype.storm.Config;
import backtype.storm.Constants;
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
  
  long partialCount = 0L;
  long totalCount = 0L;
  
  private OutputCollector collector;
  
  @Override
  public void prepare(Map stormConf, TopologyContext context, OutputCollector collector) {
    partialCount = 0L;
    this.collector = collector;
  }
  
  @Override
  public void execute(Tuple tuple) {
    //emit partialCount on each TickTuple
    if(isTickTuple(tuple))
    {
      //only emit if partialCount > 0
      if(partialCount > 0) {
        logger.info("emitting count" + 
            ", partialCount: " + partialCount + 
            ", totalCount: " + totalCount);
        collector.emit(new Values(partialCount));
        partialCount = 0L;
      }
    }
    else
    {
      partialCount++;
      totalCount++;
    }
    collector.ack(tuple);
  }

  @Override
  public Map<String,Object>  getComponentConfiguration() {
    Config conf = new Config();
    //set the TickTuple frequency to 1 second
    conf.put(Config.TOPOLOGY_TICK_TUPLE_FREQ_SECS, 1);
    return conf;
  }
  
  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
    declarer.declare(new Fields("partialCount"));
  }
  
  public static boolean isTickTuple(Tuple tuple) {
    return tuple.getSourceComponent().equals(Constants.SYSTEM_COMPONENT_ID)
        && tuple.getSourceStreamId().equals(Constants.SYSTEM_TICK_STREAM_ID);
  }
}