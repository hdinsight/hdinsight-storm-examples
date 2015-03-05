//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

function bulkImport(docs, upsert) {
	var collection = getContext().getCollection();
    var collectionLink = collection.getSelfLink();

    // The count of imported docs, also used as current doc index.
    var count = 0;
    var errorCodes = { CONFLICT: 409 };
    
    // Validate input.
    if (!docs) throw new Error("The array is undefined or null.");

    var docsLength = docs.length;
    if (docsLength == 0) {
		getContext().getResponse().setBody(0);
     	return;
    }

    // Call the create API to create a document.
    tryCreate(docs[count], callback);
    
    // Note that there are 2 exit conditions:
    // 1) The createDocument request was not accepted.
    // In this case the callback will not be called, we just call
    // setBody and we are done.
    // 2) The callback was called docs.length times.
    // In this case all documents were created and we don’t need to call
    // tryCreate anymore. Just call setBody and we are done.
    function tryCreate(doc, callback) {
    	var isAccepted = collection.createDocument(collectionLink, doc, callback);

    	// If the request was accepted, callback will be called.
    	// Otherwise report current count back to the client,
    	// which will call the script again with remaining set of docs.
    	if (!isAccepted) getContext().getResponse().setBody(count); 
    }
            
    // To replace the document, first issue a query to find it and then call replace.
    function tryReplace(doc, callback) {
    	var parsedDoc = JSON.parse(doc);
    	retrieveDoc(parsedDoc, null, function(retrievedDocs){
    		var isAccepted = collection.replaceDocument(retrievedDocs[0]._self, parsedDoc, callback);
    		if (!isAccepted) getContext().getResponse().setBody(count);
    	});
    }
    
    function retrieveDoc(doc, continuation, callback) {
    	var query = "select * from root r where r.id = '" + doc.id + "'";
    	var requestOptions = { continuation : continuation }; 
        collection.queryDocuments(collectionLink, query, requestOptions, function(err, retrievedDocs, responseOptions) {
        	if (err) throw err;
    		
    		if (retrievedDocs.length > 0) {
    			callback(retrievedDocs);
        	} else if (responseOptions.continuation) {
				retrieveDoc(doc, responseOptions.continuation, callback);        	
        	} else {
        		throw "Error in retrieving document: " + doc.id;
        	}
      	});
    }

    // This is called when collection.createDocument is done in order to
    // process the result.
    function callback(err, doc, options) {
    	if (err) {
            if (errorCodes.TOOMANYREQUESTS) { 
                getContext().getResponse().setBody(count);
                return;
            }
	    	// Replace the document if status code is 409 and upsert is enabled
	        if(upsert && err.number == errorCodes.CONFLICT) {
	        	return tryReplace(docs[count], callback);
	        } else {
	        	throw err;
	        }
    	}
    	
   		// One more document has been inserted, increment the count.
      	count++;
      	if (count >= docsLength) {
    		// If we created all documents, we are done. Just set the response.
        	getContext().getResponse().setBody(count); 
      	} else {
    		// Create next document.
        	tryCreate(docs[count], callback);
      	} 
    } 
 }
