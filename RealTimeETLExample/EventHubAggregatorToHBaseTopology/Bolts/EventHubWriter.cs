using EventHubAggregatorToHBaseTopology.Common;
using Microsoft.SCP;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubAggregatorToHBaseTopology.Bolts
{
    /// <summary>
    /// A SCP.Net C# bolt for writing into EventHub
    /// This implementation requires as many bolt tasks as number of partitions
    /// </summary>
    public class EventHubWriter : ISCPBolt
    {
        Context context;
        AppConfig appConfig;

        Stopwatch globalStopwatch;
        Stopwatch emitStopwatch;

        EventHubClient eventHubClient;
        EventHubSender eventHubSender;

        long global_error_count = 0;
        long global_emit_count = 0;

        bool ackEnabled = false;

        List<Task> tasks = new List<Task>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tablename"></param>
        public EventHubWriter(Context context, Dictionary<string, Object> parms = null)
        {
            this.context = context;
            this.appConfig = new AppConfig();

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });

            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            TopologyContext topologyContext = Context.TopologyContext;
            if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
            {
                Context.Logger.Info("EventHubWriter TopologyContext info:");
                Context.Logger.Info("TaskId: {0}", topologyContext.GetThisTaskId());
                var taskIndex = topologyContext.GetThisTaskIndex();
                Context.Logger.Info("TaskIndex: {0}", taskIndex);
                string componentId = topologyContext.GetThisComponentId();
                Context.Logger.Info("ComponentId: {0}", componentId);
                List<int> componentTasks = topologyContext.GetComponentTasks(componentId);
                Context.Logger.Info("ComponentTasks: {0}", componentTasks.Count);
            }

            InitializeEventHub();

            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                ackEnabled = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }

            globalStopwatch = new Stopwatch();
            globalStopwatch.Start();

            emitStopwatch = new Stopwatch();
            emitStopwatch.Start();
        }

        public void InitializeEventHub()
        {
            var builder = new ServiceBusConnectionStringBuilder();
            builder.Endpoints.Add(new Uri("sb://" + this.appConfig.EventHubNamespace + "." + this.appConfig.EventHubFqnAddress));
            builder.EntityPath = this.appConfig.EventHubEntityPath;
            builder.SharedAccessKeyName = this.appConfig.EventHubSharedAccessKeyName;
            builder.SharedAccessKey = this.appConfig.EventHubSharedAccessKey;
            builder.TransportType = TransportType.Amqp;

            Context.Logger.Info("EventHubWriter: ConnectionString = {0} ParitionId = {1}",
                builder.ToString(), Context.TopologyContext.GetThisTaskIndex());

            eventHubClient = EventHubClient.CreateFromConnectionString(builder.ToString());
            //TODO: Implement a distribution strategy of partitions in case number of bolt tasks is less than partitions in EventHub
            eventHubSender = eventHubClient.CreatePartitionedSender(Context.TopologyContext.GetThisTaskIndex().ToString());
        }

        /// <summary>
        /// The execute method for tuple received
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            try
            {
                var task = eventHubSender.SendAsync(new EventData(Encoding.UTF8.GetBytes(tuple.GetString(0))));
                
                if (ackEnabled)
                {
                    tasks.Add(task);
                    this.context.Ack(tuple);
                    if (tasks.Count >= 100)
                    {
                        Context.Logger.Info("Total tasks in waiting = {0}", tasks.Count);
                        Task.WaitAll(tasks.ToArray());
                        tasks.Clear();
                        Context.Logger.Info("All waiting tasks completed successfully!", tasks.Count);
                    }
                }

                global_emit_count++;

                if (global_emit_count % 5000 == 0)
                {
                    Context.Logger.Info("Total events sent to EventHub = {0} ({1} events/sec)", global_emit_count, global_emit_count / globalStopwatch.Elapsed.TotalSeconds);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Error("Failed to send tuples. Last Id = {0}, Value = {1}. Tasks = {2}", tuple.GetTupleId(), tuple.GetString(0), tasks.Count);
                Context.Logger.Error("Error Details: {0}", ex.ToString());
                if (ackEnabled)
                {
                    this.context.Fail(tuple);
                }
                global_error_count++;
                if (global_error_count > 10)
                {
                    Context.Logger.Error("High error count: {0}", global_error_count);
                    throw;
                }
            }
        }

        /// <summary>
        /// A delegate method to return the instance of this class
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public static EventHubWriter Get(Context context, Dictionary<string, Object> parms)
        {
            return new EventHubWriter(context, parms);
        }
    }
}
