using HDInsightStormExamples.Spouts;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.SCP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace HDInsightStormExamples.Bolts
{
    /// <summary>
    /// OVERVIEW:
    /// DocumentDbBolt - A SCP.Net C# bolt for writing into Azure DocumentDb
    /// 
    /// PRE-REQUISITES:
    /// 1. Azure DocumentDb - All values need to be specifed in AppSettings section of your App.Config
    ///   a. DocumentDbEndPointUrl
    ///   b. DocumentDbAuthorizationKey
    ///   c. DocumentDbDatabase
    ///   d. DocumentDbCollection
    /// 
    /// NUGET: 
    /// 1. Microsoft.SCP.Net.SDK - http://www.nuget.org/packages/Microsoft.SCP.Net.SDK/
    /// 2. Microsoft.Azure.Documents.Client - http://www.nuget.org/packages/Microsoft.Azure.Documents.Client/0.9.2-preview
    /// 3. Newtonsoft.Json - http://www.nuget.org/packages/Newtonsoft.JSON/
    /// 
    /// REFERENCES:
    /// 1. https://github.com/Azure/azure-documentdb-net/blob/master/tutorials/get-started/src/Program.cs
    /// </summary>
    public class DocumentDbBolt : ISCPBolt
    {
        Context context;
        bool enableAck = false;

        List<SCPTuple> cachedTuples = new List<SCPTuple>();

        DocumentClient documentClient { get; set; }
        DocumentCollection documentCollection { get; set; }

        string DocumentDbEndPointUrl { get; set; }
        string DocumentDbAuthorizationKey { get; set; }
        string DocumentDbDatabase { get; set; }
        string DocumentDbCollection { get; set; }

        public DocumentDbBolt(Context context)
        {
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the incoming tuples from the downstream spout or bolt tasks
            //You will also need to declare the schema for the any outgoing tuples to the upstream spout or bolt tasks
            //If there are no outgoing tuples, you can set outputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });
            //Or, something like this if you have multiple fields
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(int), typeof(DateTime), typeof(string) });
            //Another way is to have the OutputFieldTypes list exposed from Spout or Bolt to make it easy to tie with bolts that will consume it
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, IISLogGeneratorSpout.OutputFieldTypes);
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpout.OutputFieldTypes);

            //Declare both input and output schemas
            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            //TODO: Uncomment if using in hybrid mode (Java -> C#) - We need to Deserialize Java objects into C# objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaSerializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" } )
            //If the incoming tuples have only string fields, this is optional
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            //If this task excepts acks we need to set enableAck as true in TopologyBuilder for it
            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);

            InitializeDocumentDb();
        }

        /// <summary>
        /// A delegate method to return the instance of this class
        /// </summary>
        /// <param name="context">SCP Context, automatically passed by SCP.Net</param>
        /// <param name="parms"></param>
        /// <returns>An instance of the current class</returns>
        public static DocumentDbBolt Get(Context context, Dictionary<string, Object> parms)
        {
            return new DocumentDbBolt(context);
        }

        /// <summary>
        /// Initialize the DocumentDb settings and connections
        /// </summary>
        public void InitializeDocumentDb()
        {
            this.DocumentDbEndPointUrl = ConfigurationManager.AppSettings["DocumentDbEndPointUrl"];
            if (String.IsNullOrWhiteSpace(this.DocumentDbEndPointUrl))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "DocumentDbEndPointUrl");
            }

            this.DocumentDbAuthorizationKey = ConfigurationManager.AppSettings["DocumentDbAuthorizationKey"];
            if (String.IsNullOrWhiteSpace(this.DocumentDbAuthorizationKey))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "DocumentDbAuthorizationKey");
            }

            this.DocumentDbDatabase = ConfigurationManager.AppSettings["DocumentDbDatabase"];
            if (String.IsNullOrWhiteSpace(this.DocumentDbDatabase))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "DocumentDbDatabase");
            }

            this.DocumentDbCollection = ConfigurationManager.AppSettings["DocumentDbCollection"];
            if (String.IsNullOrWhiteSpace(this.DocumentDbCollection))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "DocumentDbCollection");
            }

            // Create a new instance of the DocumentClient
            documentClient = new DocumentClient(new Uri(this.DocumentDbEndPointUrl), this.DocumentDbAuthorizationKey);

            // Check to verify if database already exists
            Database database = documentClient.CreateDatabaseQuery().
                Where(db => db.Id == this.DocumentDbDatabase).AsEnumerable().FirstOrDefault();

            if (database == null)
            {
                Context.Logger.Info("Creating a new DocumentDb database: {0}", this.DocumentDbDatabase);
                // Create a database
                var task = documentClient.CreateDatabaseAsync(
                    new Database
                    {
                        Id = this.DocumentDbDatabase
                    });

                task.Wait();
                database = task.Result;
            }
            else
            {
                Context.Logger.Info("Found an existing DocumentDb database: {0}", database.Id);
            }

            // Check to verify a document collection already exists
            documentCollection = documentClient.CreateDocumentCollectionQuery(database.CollectionsLink).
                Where(c => c.Id == this.DocumentDbCollection).AsEnumerable().FirstOrDefault();

            if (documentCollection == null)
            {
                Context.Logger.Info("Creating a new DocumentDb collection: {0}", this.DocumentDbCollection);
                // Create a document collection
                var task = documentClient.CreateDocumentCollectionAsync(database.CollectionsLink,
                    new DocumentCollection
                    {
                        Id = this.DocumentDbCollection,
                    });

                task.Wait();
                documentCollection = task.Result;
            }
            else
            {
                Context.Logger.Info("Found an existing DocumentDb collection: {0}", documentCollection.Id);
            }
        }

        /// <summary>
        /// The execute method for incoming tuples
        /// </summary>
        /// <param name="tuple">The incoming tuple</param>
        public void Execute(SCPTuple tuple)
        {
            try
            {
                var values = tuple.GetValues();
                foreach (var value in values)
                {
                    Context.Logger.Info("Creating document: {0} with value: {1}", value, JsonConvert.SerializeObject(value));
                    var task = documentClient.CreateDocumentAsync(documentCollection.DocumentsLink, value);
                    task.Wait();
                    Context.Logger.Info("Document creation result status: {0}", task.Result.StatusCode);
                }

                //Ack the tuple if enableAck is set to true in TopologyBuilder. 
                //This is mandatory if the downstream bolt or spout expects an ack.
                if (enableAck)
                {
                    this.context.Ack(tuple);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Error("An error occured while executing Tuple Id: {0}. Exception Details:\r\n{1}",
                    tuple.GetTupleId(), ex.ToString());

                //Fail the tuple if enableAck is set to true in TopologyBuilder so that the tuple is replayed.
                if (enableAck)
                {
                    this.context.Fail(tuple);
                }
            }
        }
    }
}
