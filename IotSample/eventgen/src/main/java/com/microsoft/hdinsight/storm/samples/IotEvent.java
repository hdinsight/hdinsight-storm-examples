package com.microsoft.hdinsight.storm.samples;

import org.json.JSONObject;
import java.util.Random;

public class IotEvent {
  public String vin = "";
  public int outsideTemperature;
  public int engineTemperature;
  public int speed;
  public long timestamp;
  
  public IotEvent(String vin) {
    this.vin = vin;
    this.timestamp = System.currentTimeMillis();
  }
  
  /**
   * Generate evenly distributed random temperature and speed:
   * outsideTemperature: 0 - 100
   * engineTemperature: 180 - 280
   * speed: 0 - 100
   * @param r: object of Random
   */
  void randomize(Random r) {
    outsideTemperature = r.nextInt(100);
    engineTemperature = 180 + r.nextInt(100);
    speed = r.nextInt(100);
  }
  
  String serialize() {
    StringBuilder sb = new StringBuilder();
    sb.append("{");
    
    sb.append("vin:\"");
    sb.append(vin);
    sb.append("\",");
    
    sb.append("outsideTemperature:");
    sb.append(outsideTemperature);
    sb.append(",");
    
    sb.append("engineTemperature:");
    sb.append(engineTemperature);
    sb.append(",");
    
    sb.append("speed:");
    sb.append(speed);
    sb.append(",");
    
    sb.append("timestamp:");
    sb.append(timestamp);
    //sb.append("\",");
    
    sb.append("}");
    
    return sb.toString();
  }
  
  void deserialize(String data) {
    JSONObject obj = new JSONObject(data);
    vin = obj.getString("vin");
    outsideTemperature = obj.getInt("outsideTemperature");
    engineTemperature = obj.getInt("engineTemperature");
    speed = obj.getInt("speed");
    timestamp = obj.getLong("timestamp");
  }
}
