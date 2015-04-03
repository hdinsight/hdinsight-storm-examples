using Microsoft.AspNet.SignalR.Client;
using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace HDInsightStormExamples.Bolts
{
    /// <summary>
    /// OVERVIEW:
    /// SignalRBroadcastBolt - A bolt that send updates to a SignalR website.
    /// 
    /// PRE-REQUISITES:
    /// 1. SignalR website on Microsoft Azure
    ///   a. SignalRWebsiteUrl
    ///   b. SignalRHub
    ///   c. SignalRMethod
    ///   
    /// ASSUMPTIONS:
    /// 1. You need to setup the authentication as per your requirements
    /// 
    /// NUGET: 
    /// 1. SCP.Net - http://www.nuget.org/packages/Microsoft.SCP.Net.SDK/
    /// 2. Sql Transient Fault handling - http://www.nuget.org/packages/EnterpriseLibrary.TransientFaultHandling.Data
    /// 3. Newtonsoft.Json - http://www.nuget.org/packages/Newtonsoft.JSON/
    /// 
    /// REFERENCES:
    /// 1. Reliably connect to Azure SQL Database - https://msdn.microsoft.com/en-us/library/azure/dn864744.aspx
    /// </summary>
    public class SignalRBroadcastBolt : ISCPBolt
    {
        Context context;
        bool enableAck = false;

        //SignalR Connection
        HubConnection hubConnection;
        IHubProxy hubProxy;

        string SignalRWebsiteUrl { get; set; }
        string SignalRHub { get; set; }
        string SignalRMethod { get; set; }

        //Constructor
        public SignalRBroadcastBolt(Context context)
        {
            Context.Logger.Info(this.GetType().Name + " constructor called");
            //Set context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the incoming tuples from the downstream spout or bolt tasks
            //You will also need to declare the schema for the any outgoing tuples to the upstream spout or bolt tasks
            //If there are no outgoing tuples, you can set outputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            //Input schema counter updates
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(long), typeof(string) });

            //Declare both incoming and outbound schemas
            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            //TODO: Uncomment if using in hybrid mode (Java -> C#) - We need to Deserialize Java objects into C# objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaSerializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" } )
            //If the incoming tuples have only string fields, this is optional
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);

            InitializeSignalR();
        }

        /// <summary>
        /// A delegate method to return the instance of this class
        /// </summary>
        /// <param name="context">SCP Context, automatically passed by SCP.Net</param>
        /// <param name="parms"></param>
        /// <returns>An instance of the current class</returns>
        public static SignalRBroadcastBolt Get(Context context, Dictionary<string, Object> parms)
        {
            return new SignalRBroadcastBolt(context);
        }


        /// <summary>
        /// Initialize the SignalR settings and connections
        /// </summary>
        public void InitializeSignalR()
        {
            this.SignalRWebsiteUrl = ConfigurationManager.AppSettings["SignalRWebsiteUrl"];
            this.SignalRHub = ConfigurationManager.AppSettings["SignalRHub"];
            this.SignalRMethod = ConfigurationManager.AppSettings["SignalRMethod"];

            StartSignalRHubConnection();
        }

        public void Execute(SCPTuple tuple)
        {
            try
            {
                if (hubConnection.State != ConnectionState.Connected)
                {
                    hubConnection.Stop();
                    StartSignalRHubConnection();
                }

                var values = tuple.GetValues();
                hubProxy.Invoke(this.SignalRMethod, values);

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

        private void StartSignalRHubConnection()
        {
            this.hubConnection = new HubConnection(this.SignalRWebsiteUrl);
            this.hubProxy = hubConnection.CreateHubProxy(this.SignalRHub);
            hubConnection.Start().Wait();
        }
    }
}
