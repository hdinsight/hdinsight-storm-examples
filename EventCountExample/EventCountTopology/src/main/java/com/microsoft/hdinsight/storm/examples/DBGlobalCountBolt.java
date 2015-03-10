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
  
    private long curCountForDB;
  private SqlDb db;
  private String connectionStr;
  
  public DBGlobalCountBolt(String connectionStr) {
    this.connectionStr = connectionStr;
  }
  
  @Override
  public void prepare(Map config, TopologyContext context) {
    curCountForDB = 0;
    db = new SqlDb(connectionStr);
    db.dropTable();
    db.createTable();
  }
  
  @Override
  public void cleanup() {
    db.closeDbConnection();
  }

  @Override
  public void execute(Tuple tuple, BasicOutputCollector collector) {
    if (isTickTuple(tuple)) {
      logger.info("updating database, curCount: " + curCountForDB);
      db.insertValue(System.currentTimeMillis(), curCountForDB);
      curCountForDB = 0;
    }
    else {
      //int partial = (Integer)tuple.getValueByField("partial_count");
      int partial = tuple.getInteger(0);
      curCountForDB += partial;
    }
  }

  @Override
  public void declareOutputFields(OutputFieldsDeclarer declarer) {
  }

  @Override
  public Map<String,Object>  getComponentConfiguration() {
    Config conf = new Config();
    conf.put(Config.TOPOLOGY_TICK_TUPLE_FREQ_SECS, 1);
    return conf;
  }

  private static boolean isTickTuple(Tuple tuple) {
    return tuple.getSourceComponent().equals(Constants.SYSTEM_COMPONENT_ID)
        && tuple.getSourceStreamId().equals(Constants.SYSTEM_TICK_STREAM_ID);
  }
}
