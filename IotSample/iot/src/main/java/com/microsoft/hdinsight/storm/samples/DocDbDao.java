package com.microsoft.hdinsight.storm.samples;

import java.util.List;

import com.microsoft.azure.documentdb.*;
import com.google.gson.Gson;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Basic connector to document DB for create/query document
 *
 */
public class DocDbDao {
  private static final Logger logger = LoggerFactory
      .getLogger(DocDbDao.class);
  
  protected static String databaseId;
  protected static String collectionId;
  
  protected static DocumentClient documentClient;
  protected static Database databaseCache;
  protected static DocumentCollection collectionCache;
  protected static Gson gson;
  
  public DocDbDao(DocumentClient docClient, String database, String collection) {
    documentClient = docClient;
    databaseId = database;
    collectionId = collection;
    gson = new Gson();
  }
  
  protected Database getDatabase() {
    if(databaseCache == null) {
      String queryString = "SELECT * FROM root r WHERE r.id='" + databaseId + "'";
      logger.info("getDatabase: " + queryString);
      List<Database> databaseList = documentClient
          .queryDatabases(queryString, null)
          .getQueryIterable().toList();
      if(databaseList.size() > 0) {
        databaseCache = databaseList.get(0);
      }
      else {
        //create database if not exist
        try {
          logger.info("about to create database");
          Database db = new Database();
          db.setId(databaseId);
          databaseCache = documentClient.createDatabase(db,  null).getResource();
          logger.info("done creating database");
        }
        catch(DocumentClientException e) {
          e.printStackTrace();
        }
      }
    }
    return databaseCache;
  }
  
  protected DocumentCollection getCollection() {
    if(collectionCache == null) {
      String queryString = "SELECT * FROM root r WHERE r.id='" + collectionId + "'";
      logger.info("getCollection: " + queryString);
      List<DocumentCollection> collectionList = documentClient
          .queryCollections(getDatabase().getSelfLink(), queryString, null)
          .getQueryIterable().toList();
      if(collectionList.size() > 0) {
        collectionCache = collectionList.get(0);
      }
      else {
        try {
          logger.info("about to create collection");
          DocumentCollection dc = new DocumentCollection();
          dc.setId(collectionId);
          collectionCache = documentClient.createCollection(
              getDatabase().getSelfLink(), dc, null).getResource();
          logger.info("done creating collection");
        }
        catch(DocumentClientException e) {
          e.printStackTrace();
        }
      }
    }
    return collectionCache;
  }
  
  public boolean createDocument(Object obj) {
    Document doc = new Document(gson.toJson(obj));
    //doc.set("entityType", entityType);
    try {
      documentClient.createDocument(getCollection().getSelfLink(), 
          doc, null, false).close();
    }
    catch(DocumentClientException e) {
      e.printStackTrace();
      return false;
    }
    catch(Exception e) {
      e.printStackTrace();
      return false;
    }
    return true;
  }
  
  
  public <T> T queryDocument(String field, String value, Class<T> t) {
    String query = "SELECT * FROM root r WHERE r[\"" + field + "\"]='" + value + "'";
    //logger.info("query: " + query);
    List<Document> documentList = documentClient.queryDocuments(
        getCollection().getSelfLink(), query, null).getQueryIterable().toList();
    if(documentList.size() > 0) {
      String jsonStr = documentList.get(0).toString();
      //logger.info("got " + jsonStr);
      return gson.fromJson(jsonStr, t);
    }
    else {
      logger.info("no results found");
    }
    return null;
  }
  
  public boolean deleteCollection() {
    if(collectionCache == null) {
      String queryString = "SELECT * FROM root r WHERE r.id='" + collectionId + "'";
      logger.info(queryString);
      List<DocumentCollection> collectionList = documentClient
          .queryCollections(getDatabase().getSelfLink(), queryString, null)
          .getQueryIterable().toList();
      if(collectionList.size() > 0) {
        collectionCache = collectionList.get(0);
      }
    }
    
    if(collectionCache != null) {
      try {
        logger.info("about to delete collection");
        documentClient.deleteCollection(collectionCache.getSelfLink(), null);
        logger.info("done deleting collection");
        collectionCache = null;
      }
      catch(DocumentClientException e) {
        e.printStackTrace();
        return false;
      }
    }
    return true;
  }
}
