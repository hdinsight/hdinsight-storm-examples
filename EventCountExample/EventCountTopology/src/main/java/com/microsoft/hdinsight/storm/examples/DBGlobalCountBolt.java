/*******************************************************************************
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *******************************************************************************/
package com.microsoft.hdinsight.storm.examples;

import java.util.Map;
import java.util.LinkedList;

import backtype.storm.Config;
import backtype.storm.Constants;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import backtype.storm.task.OutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseRichBolt;
import backtype.storm.tuple.Tuple;
import backtype.storm.tuple.Values;

/**
 * Globally count number of messages
 */
public class DBGlobalCountBolt extends BaseRichBolt {
  private static final long serialVersionUID = 1L;
  private static final Logger logger = LoggerFactory
      .getLogger(DBGlobalCountBolt.class);

  private long finalCount = 0L;
  private long totalCount = 0L;
  
  private LinkedList<Tuple> tuplesToAck = new LinkedList<Tuple>();
  
  private SqlDb db;
  private String connectionStr;
  private String tableName = "EventCount";
  
  private int tickTupleFreqSecs = 5;
  
  private OutputCollector collector;
  
  Boolean enableAck = false;
  
  public DBGlobalCountBolt(Boolean enableAck, String connectionStr, String table) {
    this(enableAck, connectionStr, table, 5);
  }
  
  public DBGlobalCountBolt(Boolean enableAck, String connectionStr, String table, int tickTupleFreq) {
    this.enableAck = enableAck;
    logger.info("enableAck = " + enableAck);
    this.tickTupleFreqSecs = tickTupleFreq;
    logger.info("tickTupleFreqSecs = " + tickTupleFreqSecs);
    
    this.connectionStr = connectionStr;
    logger.info("connectionStr = " + this.connectionStr);
    this.tableName = table;
    logger.info("tableName = " + this.tableName);
  }
  
  @Override
  public void prepare(Map config, TopologyContext context, OutputCollector collector) {
    if(connectionStr != null && !connectionStr.isEmpty()) {
      logger.info("prepare - connectionStr = " + connectionStr + ", tableName = " + this.tableName);
      db = new SqlDb(connectionStr, tableName);
      db.dropTable();
      db.createTable();
    }
    finalCount = 0L;
    totalCount = 0L;
    this.collector = collector;
  }
  
  @Override
  public void cleanup() {
    db.closeDbConnection();
  }

  @Override
  public void execute(Tuple tuple) {
    //write the finalCount to SqlDb on each TickTuple.
    if (isTickTuple(tuple)) {
      //only emit if finalCount > 0
      if(finalCount > 0) {
        logger.info("emitting final count" + 
            ", finalCount: " + finalCount + 
            ", totalCount: " + totalCount + 
            ", tuplesToAck: " + tuplesToAck.size() + 
            " at " + System.currentTimeMillis());
        if(db != null) {
          db.insertValue(System.currentTimeMillis(), finalCount);
        } else {
          if(enableAck) {
            this.collector.emit(tuplesToAck, new Values(finalCount));
          } else {
            this.collector.emit(new Values(finalCount));
          }
        }
        
        finalCount = 0L;
        if(enableAck) {
          for(Tuple tupleToAck : tuplesToAck)
          {
            collector.ack(tupleToAck);
          }
          tuplesToAck.clear();
        }
      }
    }
    else
    {
      //Merge finalCount from all PartialCountBolt tasks
      long incomingPartialCount = tuple.getLong(0);
      finalCount += incomingPartialCount;
      totalCount += incomingPartialCount;
      if(enableAck) {
        tuplesToAck.add(tuple);
      }
    }
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
  }

  @Override
  public Map<String,Object>  getComponentConfiguration() {
    Config conf = new Config();
    //set the TickTuple frequency
    conf.put(Config.TOPOLOGY_TICK_TUPLE_FREQ_SECS, tickTupleFreqSecs);
    return conf;
  }
  
  public static boolean isTickTuple(Tuple tuple) {
    return tuple.getSourceComponent().equals(Constants.SYSTEM_COMPONENT_ID)
        && tuple.getSourceStreamId().equals(Constants.SYSTEM_TICK_STREAM_ID);
  }
}