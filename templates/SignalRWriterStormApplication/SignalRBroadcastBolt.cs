using Microsoft.AspNet.SignalR.Client;
using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace SignalRWriterStormApplication
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
    /// 2. Newtonsoft.Json - http://www.nuget.org/packages/Newtonsoft.JSON/
    /// 3. Microsoft.AspNet.SignalR.Client - http://www.nuget.org/packages/Microsoft.AspNet.SignalR.Client/
    /// 
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
            //Set context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the incoming tuples from the downstream spout or bolt tasks
            //You will also need to declare the schema for the any outgoing tuples to the upstream spout or bolt tasks
            //If there are no outgoing tuples, you can set outputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            //Input schema counter updates
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(long), typeof(long) });

            //Declare both incoming and outbound schemas
            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

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
