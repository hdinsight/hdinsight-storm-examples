package com.microsoft.hdinsight.storm.examples;

import java.util.Map;

import backtype.storm.task.OutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseRichBolt;
import backtype.storm.tuple.Fields;
import backtype.storm.tuple.Tuple;
import backtype.storm.tuple.Values;

import org.json.JSONObject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/*
 * Convert the EventHubs message into a tuple of multiple fields
 */
public class TypeConversionBolt extends BaseRichBolt {
  private static final long serialVersionUID = 1L;
  private static final Logger logger = LoggerFactory
      .getLogger(TypeConversionBolt.class); 
  private OutputCollector collector;
  
  @Override
  public void prepare(Map stormConf, TopologyContext context, OutputCollector collector) {
    this.collector = collector;
  }
  
  @Override
  public void execute(Tuple tuple) {
    try {
      JSONObject obj = new JSONObject(tuple.getString(0));
      String vin = obj.getString("vin");
      int outsideTemperature = obj.getInt("outsideTemperature");
      int engineTemperature = obj.getInt("engineTemperature");
      int speed = obj.getInt("speed");
      long timestamp = obj.getLong("timestamp");
      collector.emit(tuple, new Values(vin, outsideTemperature, engineTemperature, speed, timestamp));
      //logger.info("got vin " + vin);
    }
    catch(Exception e) {
      logger.error("Failed in parsing " + tuple.getMessageId().toString()
          + " error: " + e.getMessage());
    }
    collector.ack(tuple);
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
    declarer.declare(new Fields("vin", "outside_temp", "engine_temp", "speed", "timestamp"));
  }
}
