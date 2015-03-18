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

import backtype.storm.Config;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import backtype.storm.task.TopologyContext;
import backtype.storm.topology.BasicOutputCollector;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseBasicBolt;
import backtype.storm.tuple.Tuple;

/**
 * Globally count number of messages
 */
public class DBGlobalCountBolt extends BaseBasicBolt {
  private static final long serialVersionUID = 1L;
  private static final Logger logger = LoggerFactory
      .getLogger(DBGlobalCountBolt.class);

  private long curCountForDB = 0L;
  private long totalCount = 0L;
  private long finalCount = 102400000L;
  
  private SqlDb db;
  private String connectionStr;
  
  private long start;
  
  public DBGlobalCountBolt(String connectionStr, long finalCount) {
    this.connectionStr = connectionStr;
    this.finalCount = finalCount;
  }
  
  @Override
  public void prepare(Map config, TopologyContext context) {
    logger.info("finalCount: " + finalCount);
    db = new SqlDb(connectionStr);
    db.dropTable();
    db.createTable();
    curCountForDB = 0L;
    totalCount = 0L;
    start = System.currentTimeMillis();
  }
  
  @Override
  public void cleanup() {
    db.closeDbConnection();
  }

  @Override
  public void execute(Tuple tuple, BasicOutputCollector collector) {
    //Merge partialCount from all EventCountPartialCountBolt 
    long partialCount = tuple.getLong(0);
    curCountForDB += partialCount;
    totalCount += partialCount;
    
    //Write the curCountForDB every second. 
    //Specially handle the end of stream using finalCount so that this bolt flushes the count on last expected tuple
    //Alternatively you can also use TickTuple to create this 1 second rolling window
    //We chose not to use it to have a parity between the Java and SCP.Net for now
    //TODO: In future when SCP.Net will support TickTuple, we should revisit and change this back to using TickTuple
    if (((System.currentTimeMillis() - start) >= 1000L) || (totalCount >= finalCount)) {
      logger.info("updating database" + 
          ", curCountForDB: " + curCountForDB + 
          ", totalCount: " + totalCount + 
          ", finalCount: " + finalCount);
      db.insertValue(System.currentTimeMillis(), curCountForDB);
      curCountForDB = 0L;
      start = System.currentTimeMillis();
    }
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
  }

  @Override
  public Map<String,Object>  getComponentConfiguration() {
    Config conf = new Config();
    return conf;
  }
}
