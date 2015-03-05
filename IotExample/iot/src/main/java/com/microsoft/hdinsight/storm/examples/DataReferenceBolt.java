package com.microsoft.hdinsight.storm.examples;

import java.util.List;
import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.microsoft.azure.documentdb.ConnectionPolicy;
import com.microsoft.azure.documentdb.ConsistencyLevel;
import com.microsoft.azure.documentdb.DocumentClient;

import backtype.storm.task.OutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseRichBolt;
import backtype.storm.tuple.Fields;
import backtype.storm.tuple.Tuple;

class VehicleInfo {
  public String vin;
  public String model;
}

/**
 * Enrich data by referencing Document DB
 */
public class DataReferenceBolt extends BaseRichBolt {
  private static final long serialVersionUID = 1L;
  private static final Logger logger = LoggerFactory
      .getLogger(DataReferenceBolt.class);
  
  private DocDbDao dao = null;
  private String docdbHost;
  private String docdbMasterKey;
  private String docdbDatabase;
  private String docdbCollection;
  private OutputCollector collector; 
  
  public DataReferenceBolt(String docdbHost, String docdbMasterKey,
      String docdbDatabase, String docdbCollection) {
    this.docdbHost = docdbHost;
    this.docdbMasterKey = docdbMasterKey;
    this.docdbDatabase = docdbDatabase;
    this.docdbCollection = docdbCollection;
  }
  @Override
  public void prepare(Map stormConf, TopologyContext context, OutputCollector collector) {
    logger.info("preparing DataReferenceBolt for DocDb: " + docdbHost);
    DocumentClient documentClient = new DocumentClient(docdbHost, docdbMasterKey,
        ConnectionPolicy.GetDefault(), ConsistencyLevel.Session);
    dao = new DocDbDao(documentClient, docdbDatabase, docdbCollection);
    this.collector = collector;
  }
  
  @Override
  public void execute(Tuple tuple) {
    //Reference document DB to get model number for a given VIN
    List<Object> values = tuple.getValues();
    String vin = tuple.getStringByField("vin");
    
    VehicleInfo info = null;
    try {
      //To improve performance, we could implement cache to reduce the calls to Document DB
      info = dao.queryDocument("vin", vin, VehicleInfo.class);
    }
    catch(Exception e) {
      logger.error("Exception in queryDocument for VIN: " + vin);
      logger.error(e.getStackTrace().toString());
    }
    if(info != null) {
      values.add(info.model);
      collector.emit(tuple, values);
    }
    collector.ack(tuple);
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
    declarer.declare(new Fields("vin", "outside_temp", "engine_temp", "speed", "timestamp", "model"));
  }

}
