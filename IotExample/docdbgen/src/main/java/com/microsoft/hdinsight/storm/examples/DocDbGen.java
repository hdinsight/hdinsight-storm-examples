package com.microsoft.hdinsight.storm.examples;

import java.io.BufferedReader;
import java.io.FileReader;
import java.util.ArrayList;
import java.util.List;
import java.util.Properties;

import com.microsoft.azure.documentdb.ConnectionPolicy;
import com.microsoft.azure.documentdb.ConsistencyLevel;
import com.microsoft.azure.documentdb.DocumentClient;

public class DocDbGen
{
  String vinFile;
  DocDbBulkImportDao dao;
  
  public DocDbGen(String propFile, String vinFile) {
    this.vinFile = vinFile;
    //create documentDB DAO based on properties file
    try {
      Properties properties = new Properties();
      properties.load(new FileReader(propFile));
      
      String docdbHost = properties.getProperty("documentdb.host");
      String docdbMasterKey = properties.getProperty("documentdb.master.key");
      DocumentClient documentClient = new DocumentClient(docdbHost, docdbMasterKey,
          ConnectionPolicy.GetDefault(), ConsistencyLevel.Session);
      String databaseId = properties.getProperty("documentdb.database");
      String collectionId = properties.getProperty("documentdb.collection");
      dao = new DocDbBulkImportDao(documentClient, databaseId, collectionId);
    }
    catch(Exception e) {
      System.out.println("Exception in creating document: " + e.getMessage());
    }
  }
  
  public void queryVehicleDocument() {
    VehicleInfo vi = dao.queryDocument("vin", "FHL3O1SA4IEHB4WU1", VehicleInfo.class);
    System.out.println("got document: " + vi.vin + ", " + vi.model);
  }
  
  public void generateVehicleDocuments() {
    System.out.println("deleting target collection first...");
    dao.deleteCollection();
    System.out.println("creating collection and documents...");
    try {
      BufferedReader in = new BufferedReader(new FileReader(vinFile));
      List<Object> vis = new ArrayList<Object>();
      int batchno = 0;
      while(in.ready()) {
        vis.clear();
        for(int i=0; i<DocDbBulkImportDao.MAX_SCRIPT_DOCS; ++i) {
          if(!in.ready()) {
            break;
          }
          String line = in.readLine();
          String[] parts = line.split(",");
          VehicleInfo vi = new VehicleInfo();
          vi.vin = parts[0];
          vi.model = parts[1];
          //System.out.println("sending " + vi.vin);
          //dao.createDocument(vi);
          vis.add(vi);
        }
        System.out.println("sending batch " + batchno++);
        dao.bulkCreateDocument(vis);
      }
      in.close();
      System.out.println("done");
    }
    catch(Exception e) {
      System.out.println("Exception in reading vins file: " + e.getMessage());
    }
  }
    
  public static void main(String[] args) throws Exception {
    if(args.length < 2) {
      System.out.println("usage: ");
      System.out.println("\tdocdbgen docdb.properties vehiclevin.txt");
      return;
    }
    
    DocDbGen ddg = new DocDbGen(args[0], args[1]);
    ddg.generateVehicleDocuments();
    //ddg.queryVehicleDocument();
  }
}
