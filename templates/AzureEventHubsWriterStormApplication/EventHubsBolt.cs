using Microsoft.SCP;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace AzureEventHubsWriterStormApplication
{
    /// <summary>
    /// OVERVIEW:
    /// A SCP.Net C# bolt for writing into EventHub
    /// This implementation requires as many bolt tasks as number of partitions
    /// 
    /// PRE-REQUISITES:
    /// 1. Microsoft Azure EventHubs - Specify the credentials in AppSettings section of your App.Config
    /// 2. Declare the Input Schema as per your requirements - It can be one field or multiple.
    /// 
    /// NUGET:
    /// 1. Microsoft.SCP.Net.SDK - http://www.nuget.org/packages/Microsoft.SCP.Net.SDK/
    /// 2. WindowsAzure.ServiceBus - http://www.nuget.org/packages/WindowsAzure.ServiceBus/
    /// 2. Newtonsoft.Json - http://www.nuget.org/packages/Newtonsoft.Json/
    /// 
    /// ASSUMPTIONS:
    /// 1. User has configured the EventHub correctly including "scale" settings in Azure portal
    /// 2. Multiple field tuples are converted using JsonConvert
    /// 
    /// REFERENCES:
    /// 1. http://azure.microsoft.com/en-us/documentation/articles/service-bus-event-hubs-csharp-ephcs-getstarted/
    /// 2. http://azure.microsoft.com/en-us/documentation/articles/service-bus-event-hubs-csharp-storm-getstarted/
    /// </summary>
    public class EventHubBolt : ISCPBolt
    {
        Context context;
        bool enableAck = false;

        static string EventHubFqnAddress = "servicebus.windows.net";
        string EventHubNamespace { get; set; }
        string EventHubEntityPath { get; set; }
        string EventHubSharedAccessKeyName { get; set; }
        string EventHubSharedAccessKey { get; set; }
        string EventHubPartitions { get; set; }

        EventHubClient eventHubClient;
        EventHubSender eventHubSender;
        string partitionId;

        public EventHubBolt(Context context)
        {
            //Set context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the incoming tuples from the downstream spout or bolt tasks
            //You will also need to declare the schema for the any outgoing tuples to the upstream spout or bolt tasks
            //If there are no outgoing tuples, you can set outputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            //One way is to have the OutputFieldTypes list exposed from Spout or Bolt to make it easy to tie with bolts that will consume it
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, IISLogGeneratorSpout.OutputFieldTypes);
            //Or you can declare the list directly
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });

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

            InitializeEventHub();
        }

        public void InitializeEventHub()
        {
            Context.Logger.Info("Current AppConfig File: " + ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));
            Context.Logger.Info("Current AppSettings: " + String.Join(Environment.NewLine, ConfigurationManager.AppSettings.AllKeys));

            this.EventHubNamespace = ConfigurationManager.AppSettings["EventHubNamespace"];
            if (String.IsNullOrWhiteSpace(this.EventHubNamespace))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubNamespace");
            }

            this.EventHubEntityPath = ConfigurationManager.AppSettings["EventHubEntityPath"];
            if (String.IsNullOrWhiteSpace(this.EventHubEntityPath))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubEntityPath");
            }

            this.EventHubSharedAccessKeyName = ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"];
            if (String.IsNullOrWhiteSpace(this.EventHubSharedAccessKeyName))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubSharedAccessKeyName");
            }

            this.EventHubSharedAccessKey = ConfigurationManager.AppSettings["EventHubSharedAccessKey"];
            if (String.IsNullOrWhiteSpace(this.EventHubSharedAccessKey))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubSharedAccessKey");
            }

            this.EventHubPartitions = ConfigurationManager.AppSettings["EventHubPartitions"];
            if (String.IsNullOrWhiteSpace(this.EventHubPartitions))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubPartitions");
            }

            var builder = new ServiceBusConnectionStringBuilder();
            builder.Endpoints.Add(new Uri("sb://" + this.EventHubNamespace + "." + EventHubFqnAddress));
            builder.EntityPath = this.EventHubEntityPath;
            builder.SharedAccessKeyName = this.EventHubSharedAccessKeyName;
            builder.SharedAccessKey = this.EventHubSharedAccessKey;
            builder.TransportType = TransportType.Amqp;

            var partitionCount = int.Parse(this.EventHubPartitions);

            TopologyContext topologyContext = Context.TopologyContext;
            Context.Logger.Info(this.GetType().Name + " TopologyContext info:");
            Context.Logger.Info("TaskId: {0}", topologyContext.GetThisTaskId());
            var taskIndex = topologyContext.GetThisTaskIndex();
            Context.Logger.Info("TaskIndex: {0}", taskIndex);
            string componentId = topologyContext.GetThisComponentId();
            Context.Logger.Info("ComponentId: {0}", componentId);
            List<int> componentTasks = topologyContext.GetComponentTasks(componentId);
            Context.Logger.Info("ComponentTasks: {0}", componentTasks.Count);

            if (partitionCount != componentTasks.Count)
            {
                throw new Exception(
                    String.Format("Component task count does not match partition count. Component: {0}, Tasks: {1}, Partition: {2}",
                    componentId, componentTasks.Count, partitionCount));
            }

            partitionId = taskIndex.ToString();

            Context.Logger.Info(this.GetType().Name + " ConnectionString = {0}, ParitionId = {1}",
                builder.ToString(), partitionId);

            eventHubClient = EventHubClient.CreateFromConnectionString(builder.ToString());
            eventHubSender = eventHubClient.CreatePartitionedSender(partitionId);
        }

        /// <summary>
        /// The execute method for tuple received
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            try
            {
                var values = tuple.GetValues();
                var data = string.Empty;
                if (values.Count == 1)
                {
                    data = JsonConvert.SerializeObject(values[0]);
                }
                else
                {
                    data = JsonConvert.SerializeObject(values);
                }

                eventHubSender.Send(new EventData(Encoding.UTF8.GetBytes(data)));

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

        /// <summary>
        /// A delegate method to return the instance of this class
        /// </summary>
        /// <param name="context">SCP Context, automatically passed by SCP.Net</param>
        /// <param name="parms"></param>
        /// <returns>An instance of EventHubBolt class</returns>
        public static EventHubBolt Get(Context context, Dictionary<string, Object> parms)
        {
            return new EventHubBolt(context);
        }
    }
}