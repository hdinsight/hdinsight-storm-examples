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
import backtype.storm.Constants;

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

  private long partialCount = 0L;
  private long totalCount = 0L;
  
  private SqlDb db;
  private String connectionStr;
  
  public DBGlobalCountBolt(String connectionStr) {
    this.connectionStr = connectionStr;
  }
  
  @Override
  public void prepare(Map config, TopologyContext context) {
    db = new SqlDb(connectionStr);
    db.dropTable();
    db.createTable();
    partialCount = 0L;
    totalCount = 0L;
  }
  
  @Override
  public void cleanup() {
    db.closeDbConnection();
  }

  @Override
  public void execute(Tuple tuple, BasicOutputCollector collector) {
    //write the partialCount to SqlDb on each TickTuple.
    if (isTickTuple(tuple)) {
      //only emit if partialCount > 0
      if(partialCount > 0) {
        logger.info("updating database" + 
            ", partialCount: " + partialCount + 
            ", totalCount: " + totalCount);
        db.insertValue(System.currentTimeMillis(), partialCount);
        partialCount = 0L;
      }
    }
    else
    {
      //Merge partialCount from all PartialCountBolt tasks
      long incomingPartialCount = tuple.getLong(0);
      partialCount += incomingPartialCount;
      totalCount += incomingPartialCount;
    }
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
  }

  @Override
  public Map<String,Object>  getComponentConfiguration() {
    Config conf = new Config();
    //set the TickTuple frequency to 1 second
    conf.put(Config.TOPOLOGY_TICK_TUPLE_FREQ_SECS, 1);
    return conf;
  }
  
  public static boolean isTickTuple(Tuple tuple) {
    return tuple.getSourceComponent().equals(Constants.SYSTEM_COMPONENT_ID)
        && tuple.getSourceStreamId().equals(Constants.SYSTEM_TICK_STREAM_ID);
  }
}