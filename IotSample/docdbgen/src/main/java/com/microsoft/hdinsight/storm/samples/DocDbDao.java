package com.microsoft.hdinsight.storm.samples;

import java.util.List;
import com.microsoft.azure.documentdb.*;
import com.google.gson.Gson;

/**
 * Basic connector to document DB for create/query document
 *
 */
public class DocDbDao {
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
      System.out.println("getDatabase: " + queryString);
      List<Database> databaseList = documentClient
          .queryDatabases(queryString, null)
          .getQueryIterable().toList();
      if(databaseList.size() > 0) {
        databaseCache = databaseList.get(0);
      }
      else {
        //create database if not exist
        try {
          System.out.println("about to create database");
          Database db = new Database();
          db.setId(databaseId);
          databaseCache = documentClient.createDatabase(db,  null).getResource();
          System.out.println("done creating database");
        }
        catch(DocumentClientException e) {
          e.printStackTrace();
        }
      }
    }
    return databaseCache;
  }
  
  protected DocumentCollection getCollection() {
    //Collections in DocumentDB are 10GB. However, we provide the capability
    //that user passes comma separated collections and we round robin on these
    //collections for document creation.
    //Take a look at hadoop connector as an example:
    //https://github.com/Azure/azure-documentdb-hadoop/blob/master/src/com/microsoft/azure/documentdb/hadoop/DocumentDBRecordWriter.java
    
    if(collectionCache == null) {
      String queryString = "SELECT * FROM root r WHERE r.id='" + collectionId + "'";
      System.out.println("getCollection: " + queryString);
      List<DocumentCollection> collectionList = documentClient
          .queryCollections(getDatabase().getSelfLink(), queryString, null)
          .getQueryIterable().toList();
      if(collectionList.size() > 0) {
        collectionCache = collectionList.get(0);
      }
      else {
        try {
          System.out.println("about to create collection");
          DocumentCollection dc = new DocumentCollection();
          dc.setId(collectionId);
          collectionCache = documentClient.createCollection(
              getDatabase().getSelfLink(), dc, null).getResource();
          System.out.println("done creating collection");
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
    System.out.println("query: " + query);
    List<Document> documentList = documentClient.queryDocuments(
        getCollection().getSelfLink(), query, null).getQueryIterable().toList();
    if(documentList.size() > 0) {
      String jsonStr = documentList.get(0).toString();
      System.out.println("got " + jsonStr);
      return gson.fromJson(jsonStr, t);
    }
    else {
      System.out.println("no results found");
    }
    return null;
  }
  
  public boolean deleteCollection() {
    if(collectionCache == null) {
      String queryString = "SELECT * FROM root r WHERE r.id='" + collectionId + "'";
      System.out.println(queryString);
      List<DocumentCollection> collectionList = documentClient
          .queryCollections(getDatabase().getSelfLink(), queryString, null)
          .getQueryIterable().toList();
      if(collectionList.size() > 0) {
        collectionCache = collectionList.get(0);
      }
    }
    
    if(collectionCache != null) {
      try {
        System.out.println("about to delete collection");
        documentClient.deleteCollection(collectionCache.getSelfLink(), null);
        System.out.println("done deleting collection");
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
