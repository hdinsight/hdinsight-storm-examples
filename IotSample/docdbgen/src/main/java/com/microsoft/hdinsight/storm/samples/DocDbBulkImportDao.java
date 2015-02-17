//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

package com.microsoft.hdinsight.storm.samples;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

import org.apache.commons.logging.Log;
import org.apache.commons.logging.LogFactory;

import com.microsoft.azure.documentdb.Document;
import com.microsoft.azure.documentdb.DocumentClient;
import com.microsoft.azure.documentdb.DocumentClientException;
import com.microsoft.azure.documentdb.StoredProcedure;

public class DocDbBulkImportDao extends DocDbDao {
  public final static int MAX_SCRIPT_DOCS = 200;
  public final static int MAX_SCRIPT_SIZE = 50000;

  private static final Log LOG = LogFactory.getLog(DocDbBulkImportDao.class);
  private final static int REQUEST_RATE_TOO_LARGE = 429;
  private final static int STATUS_FORBIDDEN = 403;
  private final static String BULK_IMPORT_ID = "BulkImportSprocV1";
  private final static String BULK_IMPORT_PATH = "/BulkImportScript.js";

  public DocDbBulkImportDao(DocumentClient docClient, String database,
      String collection) {
    super(docClient, database, collection);
  }
  
  public boolean bulkCreateDocument(List<Object> objs) {
    try {
      StoredProcedure sproc = DocDbBulkImportDao.createBulkImportStoredProcedure(
          documentClient, getCollection().getSelfLink());
      
      List<Document> docs = new ArrayList<Document>();
      for(Object obj: objs) {
        docs.add(new Document(gson.toJson(obj)));
      }
      DocDbBulkImportDao.executeWriteStoredProcedure(
          documentClient, getCollection().getSelfLink(), sproc, docs, true);
      return true;
    }
    catch (DocumentClientException e) {
      e.printStackTrace();
      return false;
    }
  }

  /**
   * Gets the bulk import stored procedure that will be used for writing documents ( if the sproc already exists, use it, otherwise create a new one.
   * @param client the DocumentClient instance for DocumentDB.
   * @param collectionLink the self-link of the collection to write to.
   * @return StoredProcedure instance that will be used for writing
   */
  public static StoredProcedure createBulkImportStoredProcedure(DocumentClient client, String collectionLink)
      throws DocumentClientException {
    String query = String.format("select * from root r where r.id = '%s'", BULK_IMPORT_ID);
    List<StoredProcedure> sprocs = client.queryStoredProcedures(collectionLink, query, null).getQueryIterable().toList();
    if(sprocs.size() > 0) {
      return sprocs.get(0);
    }

    StoredProcedure sproc = new StoredProcedure();
    sproc.setId(BULK_IMPORT_ID);
    String sprocBody = getBulkImportBody(client);
    sproc.setBody(sprocBody);
    return client.createStoredProcedure(collectionLink, sproc, null).getResource();
  }

  /**
   * Executes the bulk import stored procedure for a list of documents.
   * The execution takes into consideration throttling and blacklisting of the stored procedure.
   * @param client The DocumentClient instance for DocumentDB
   * @param collectionSelfLink the self-link for the collection to write to.
   * @param sproc The stored procedure to execute
   * @param allDocs The list of documents to write
   * @param upsert  Specifies whether to replace the document if exists or not. By default it's true.
   */
  public static void executeWriteStoredProcedure(final DocumentClient client, String collectionSelfLink, final StoredProcedure sproc,
      List<Document> allDocs, final boolean upsert) {
    try {
      int currentCount = 0;
      int docCount = Math.min(allDocs.size(), MAX_SCRIPT_DOCS);

      while (currentCount < docCount)
      {
        String []jsonArrayString = CreateBulkInsertScriptArguments(allDocs, currentCount, docCount, MAX_SCRIPT_SIZE);
        String response = client.executeStoredProcedure(sproc.getSelfLink(), new Object[] {jsonArrayString, upsert })
            .getResponseAsString();
        int createdCount = Integer.parseInt(response);

        currentCount += createdCount;
      }
    } catch (DocumentClientException e) {
      if (e.getStatusCode() == REQUEST_RATE_TOO_LARGE) {
        LOG.error("Throttled, retrying after:"+e.getRetryAfterInMilliseconds());

        try {
          Thread.sleep(e.getRetryAfterInMilliseconds());
        } catch (InterruptedException e1) {
          throw new IllegalStateException(e1);
        }

        executeWriteStoredProcedure(client, collectionSelfLink, sproc, allDocs, upsert);
      } else {
        e.printStackTrace();
        throw new IllegalStateException(e);
      }
    }
  }

  /**
   * Used to delete the stored procedure and recreate it if it gets blacklisted
   */
  private static void recreateStoredProcedure(DocumentClient client, String collectionSelfLink, StoredProcedure sproc) {
    try {
      LOG.error("sproc got recreated after blacklisting");
      client.deleteStoredProcedure(sproc.getSelfLink(), null);
      StoredProcedure createdSproc = createBulkImportStoredProcedure(client, collectionSelfLink);
      sproc.set("_self", createdSproc.getSelfLink());
    } catch (DocumentClientException e) {
      e.printStackTrace();
      throw new IllegalStateException(e);
    }

  }

  /**
   * 
   * @param docs The list of documents to be created 
   * @param currentIndex the current index in the list of docs to start with.
   * @param maxCount the max count to be created by the sproc.
   * @param maxScriptSize the max size of the sproc that is used to avoid exceeding the max request size.
   * @return
   */
  private static String[] CreateBulkInsertScriptArguments(List<Document> docs, int currentIndex, int maxCount, int maxScriptSize)
  {
    if (currentIndex >= maxCount) return new String[]{};

    ArrayList<String> jsonDocumentList = new ArrayList<String>();
    String stringifiedDoc = docs.get(0).toString();
    jsonDocumentList.add(stringifiedDoc);
    int scriptCapacityRemaining = maxScriptSize - stringifiedDoc.length();

    int i = 1;
    while (scriptCapacityRemaining > 0 && (currentIndex + i) < maxCount)
    {
      stringifiedDoc = docs.get(currentIndex + i).toString();
      jsonDocumentList.add(stringifiedDoc);
      scriptCapacityRemaining-= stringifiedDoc.length();
      i++;
    }

    String[] jsonDocumentArray = new String[jsonDocumentList.size()];
    jsonDocumentList.toArray(jsonDocumentArray);
    return jsonDocumentArray;
  }

  /**
   * Reads the bulk import script body from the file.
   * @param client the DocumentClient instance.
   * @return a string that contains the stored procedure body.
   */
  private static String getBulkImportBody(DocumentClient client) {
    try {
      InputStream stream = DocDbBulkImportDao.class.getResourceAsStream(BULK_IMPORT_PATH);
      BufferedReader in = new BufferedReader(new InputStreamReader(stream));
      List<String> scriptLines = new ArrayList<String>();
      while(in.ready()) {
        String line = in.readLine();
        scriptLines.add(line);
      }
      StringBuilder scriptBody = new StringBuilder();
      for (Iterator<String> iterator = scriptLines.iterator(); iterator.hasNext();) {
        String line = (String) iterator.next();
        scriptBody.append(line + "\n");
      }

      return scriptBody.toString();
    } catch (IOException e) {
      throw new IllegalStateException(e);
    }
  }
}