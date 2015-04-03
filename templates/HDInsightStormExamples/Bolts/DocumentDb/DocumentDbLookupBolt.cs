using HDInsightStormExamples.Spouts;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace HDInsightStormExamples.Bolts
{
    /// <summary>
    /// OVERVIEW:
    /// DocumentDbBolt - A SCP.Net C# bolt for looking up documents from Azure DocumentDb based on incoming values
    /// The documented are then emitted by this bolt as JSON strings
    /// 
    /// PRE-REQUISITES:
    /// 1. Azure DocumentDb - All values need to be specifed in AppSettings section of your App.Config
    ///   a. DocumentDbEndPointUrl
    ///   b. DocumentDbAuthorizationKey
    ///   c. DocumentDbDatabase
    ///   d. DocumentDbCollection
    ///   e. DocumentDbLookupField - The lookup field which will be queried against
    /// 
    /// NUGET: 
    /// 1. Microsoft.SCP.Net.SDK - http://www.nuget.org/packages/Microsoft.SCP.Net.SDK/
    /// 2. Microsoft.Azure.Documents.Client - http://www.nuget.org/packages/Microsoft.Azure.Documents.Client/0.9.2-preview
    /// 3. Newtonsoft.Json - http://www.nuget.org/packages/Newtonsoft.JSON/
    /// 
    /// REFERENCES:
    /// 1. https://github.com/Azure/azure-documentdb-net/blob/master/tutorials/get-started/src/Program.cs
    /// </summary>
    public class DocumentDbLookupBolt : ISCPBolt
    {
        Context context;
        bool enableAck = false;

        DocumentClient documentClient { get; set; }
        DocumentCollection documentCollection { get; set; }

        string DocumentDbEndPointUrl { get; set; }
        string DocumentDbAuthorizationKey { get; set; }
        string DocumentDbDatabase { get; set; }
        string DocumentDbCollection { get; set; }

        string DocumentDbLookupField { get; set; }

        /// <summary>
        /// DocumentDbLookupBolt constructor where all settings w.r.t SCP.Net are configured
        /// </summary>
        /// <param name="context"></param>
        public DocumentDbLookupBolt(Context context)
        {
            Context.Logger.Info(this.GetType().Name + " constructor called");
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the incoming tuples from the downstream spout or bolt tasks
            //You will also need to declare the schema for the any outgoing tuples to the upstream spout or bolt tasks
            //If there are no outgoing tuples, you can set outputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });
            //Or, something like this if you have multiple fields
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(int), typeof(DateTime), typeof(string) });

            //Another way is to have the OutputFieldTypes list exposed from downstream Spout or Bolt to make it easy to tie with upstream Bolts that will consume it
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, IISLogGeneratorSpout.OutputFieldTypes);
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpout.OutputFieldTypes);

            //As this is a lookup bolt we will also set the outputSchema which in this case is a JSON string
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });

            //Declare both input and output schemas
            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));

            //TODO: Uncomment if using in hybrid mode (Java -> C#) - We need to Deserialize Java objects into C# objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaSerializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" } )
            //If the incoming tuples have only string fields, this is optional
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            //TODO: Uncomment if using in reverse hybrid mode (C# -> Java) - We need to Serialize C# objects into Java objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaDeserializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer" } )
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedSerializer(new CustomizedInteropJSONSerializer());

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
        public static DocumentDbLookupBolt Get(Context context, Dictionary<string, Object> parms)
        {
            return new DocumentDbLookupBolt(context);
        }

        /// <summary>
        /// Initialize the DocumentDb settings and connections
        /// </summary>
        public void InitializeDocumentDb()
        {
            this.DocumentDbEndPointUrl = ConfigurationManager.AppSettings["DocumentDbEndPointUrl"];
            if(String.IsNullOrWhiteSpace(this.DocumentDbEndPointUrl))
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

            this.DocumentDbLookupField = ConfigurationManager.AppSettings["DocumentDbLookupField"];
            if (String.IsNullOrWhiteSpace(this.DocumentDbLookupField))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "DocumentDbLookupField");
            }

            // Create a new instance of the DocumentClient
            documentClient = new DocumentClient(new Uri(this.DocumentDbEndPointUrl), this.DocumentDbAuthorizationKey);

            // Check to verify if database already exists
            Database database = documentClient.CreateDatabaseQuery().Where(db => db.Id == this.DocumentDbDatabase).AsEnumerable().FirstOrDefault();

            if (database == null)
            {
                throw new Exception("DocumentDb database '" + this.DocumentDbDatabase + "' does not exist.");
            }

            // Check to verify a document collection already exists
            documentCollection = documentClient.CreateDocumentCollectionQuery(database.CollectionsLink).Where(c => c.Id == this.DocumentDbCollection).AsEnumerable().FirstOrDefault();

            if (documentCollection == null)
            {
                throw new Exception("DocumentDb database collection '" + this.DocumentDbCollection + "' does not exist.");
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
                //Assuming that the first field of the incoming tuple has the lookup value you are interested in
                var value = tuple.GetValue(0);
                var lookupValue = value.ToString();
                IEnumerable<object> documents = null;

                if (value is string)
                {
                    documents = this.documentClient.CreateDocumentQuery(documentCollection.DocumentsLink).
                        Where(d => d.Id.Equals(value)).AsEnumerable();
                }
                else
                {
                    Context.Logger.Info("Lookup value is not a string, getting the value for the lookup field in the object.");
                    lookupValue = value.GetType().GetProperty(this.DocumentDbLookupField).GetValue(value).ToString();
                    string query = "SELECT * FROM ROOT R WHERE R[\"" + this.DocumentDbLookupField + "\"] = \"" + lookupValue + "\"";
                    Context.Logger.Info("DocumentDb Query: {0}", query);
                    documents = this.documentClient.CreateDocumentQuery(documentCollection.DocumentsLink, query).AsEnumerable();
                }

                if (documents.Count() == 0)
                {
                    Context.Logger.Info("No documents found for lookup field: {0}, lookup value: {1}", this.DocumentDbLookupField, lookupValue);
                }
                else
                {
                    foreach (var document in documents)
                    {
                        //A document is just JSON so we will call a ToString() to set the emitValue as JSON string
                        var emitValue = document.ToString();

                        Context.Logger.Info("Found document for lookup field: {0}, lookup value: {1}, document: {2}",
                            this.DocumentDbLookupField, lookupValue, emitValue);

                        if (enableAck)
                        {
                            //NOTE: For a Bolt with enableAck we need to emit with anchors - list of tuples
                            //In this scenario we are emitting per tuple so the anchor is only for this tuple
                            this.context.Emit(Constants.DEFAULT_STREAM_ID, new List<SCPTuple>() { tuple }, new Values(emitValue));
                        }
                        else
                        {
                            this.context.Emit(Constants.DEFAULT_STREAM_ID, new Values(emitValue));
                        }
                    }
                }

                //Ack the tuple if enableAck is set to true in TopologyBuilder. This is mandatory if the downstream bolt or spout expects an ack.
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
