/**
 * An utility class to write data to SQL DB using sqljdbc
 */
package com.microsoft.hdinsight.storm.examples;

import java.sql.*;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class SqlDb {
  private static final Logger logger = LoggerFactory
      .getLogger(SqlDb.class);

  // Declare the JDBC objects.
  private Connection con = null;
  private Statement stmt = null;
  private ResultSet rs = null;

  public SqlDb (String connectionStr) {
    try {
      // Establish the connection.
      logger.info("connectionStr: " + connectionStr);
      con = DriverManager.getConnection(connectionStr);
    }
    catch (Exception e) {
       logger.error("Exception", e);
    }
  }
  
  public void createTable() {
    logger.info("createTable");
    String SQL = "CREATE TABLE EHEventCount(timestamp bigint PRIMARY KEY, eventCount bigint)";

    // Create and execute an SQL statement that returns some data.
    try {
      stmt = con.createStatement();
      stmt.execute(SQL);
    }
    catch (Exception e) {
      logger.error("Exception", e);
    }
  }
  
  public void dropTable() {
    logger.info("dropTable");
    String SQL = "IF OBJECT_ID (N'dbo.EHEventCount', N'U') IS NOT NULL DROP TABLE dbo.EHEventCount";
    try {
      stmt = con.createStatement();
      stmt.execute(SQL);
    }
    catch (Exception e) {
      logger.error("Exception", e);
    }
  }
  
  public void insertValue(long timestamp, long count) {
    String SQL = "INSERT INTO EHEventCount "
        + "VALUES (" + timestamp + "," + count + ");";
    try {
      stmt = con.createStatement();
      stmt.executeUpdate(SQL);
    }
    catch (Exception e) {
      logger.error("Exception", e);
    }
  }

  public void resetTable() {
    logger.info("resetTable");
    String SQL = "Truncate Table EHEventCount";
    try {
      stmt = con.createStatement();
      rs = stmt.executeQuery(SQL);
    }
    catch (Exception e) {
      logger.error("Exception", e);
    }
  }
  
  public void closeDbConnection() {
    logger.info("closeDbConnection");
    if (rs != null) try { rs.close(); } catch(Exception e) {}
    if (stmt != null) try { stmt.close(); } catch(Exception e) {}
    if (con != null) try { con.close(); } catch(Exception e) {}
  }
}
