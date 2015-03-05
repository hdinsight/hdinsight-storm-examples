using EventHubAggregatorToHBaseTopology.Common;
using Microsoft.SCP;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubAggregatorToHBaseTopology.Bolts
{
    /// <summary>
    /// This is the primary bolt that starts to count the primary key and secondary key combinations over time
    /// The aggregation example here is of counts, which can be easily extended to Sum without lot of work, 
    /// you just have to deserialize the field as a number than count on it.
    /// </summary>
    class EventAggregator : ISCPBolt
    {
        public Context context;
        public AppConfig appConfig;

        public long global_error_count = 0;
        public long global_receive_count = 0;
        public long global_emit_count = 0;

        public long last_error_count = 0;
        public long last_receive_count = 0;
        public long last_emit_count = 0;
        public double last_emit_in_secs = 0;

        /// <summary>
        /// This variable helps in tracking the size of aggregations you have in memory to help while debugging and logging
        /// </summary>
        public long current_cache_size = 0;

        public Dictionary<DateTime, Dictionary<string, Dictionary<string, double>>> aggregatedCounts = new Dictionary<DateTime, Dictionary<string, Dictionary<string, double>>>();

        /// <summary>
        /// The primary key of your aggregations, that is count the occurences of SecondaryKey for each primary key during a aggregation window.
        /// Example 1: Consider a Web Result Log where Client IP = PrimaryKey, Http Status = SecondaryKey, 
        /// this way for each client you will get how many times a particular status code occured.
        /// Example 2: Consider the opposite example where Http Status = PrimaryKey, Client IP = SecondaryKey,
        /// this way for each http status type you will get how many times that client IP occured.
        /// Adding aggregators for each of above examples in same topology can provide you easy reverse lookups.
        /// The Example 2 extends for a great addition to a Ranking Bolt as Ranking Bolt will provide you top clients that hit a particular http status code.
        /// </summary>
        public string PrimaryKey { get; set; }
        /// <summary>
        /// The secondary key of your aggregations, this is counted for each primary key for your aggregations.
        /// </summary>
        public string SecondaryKey { get; set; }

        public Stopwatch globalstopwatch;
        public Stopwatch emitstopwatch;

        public EventAggregator()
        {
        }

        public EventAggregator(Context context, Dictionary<string, Object> parms = null)
        {
            this.context = context;

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(object) });

            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, AggregatedTupleFields);

            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));
            this.context.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            Initialize(parms);
        }

        public virtual void Initialize(Dictionary<string, Object> parms = null)
        {
            this.appConfig = new AppConfig();

            this.PrimaryKey = this.appConfig.PrimaryKey;
            this.SecondaryKey = this.appConfig.SecondaryKey;

            if (String.IsNullOrWhiteSpace(this.PrimaryKey) || String.IsNullOrWhiteSpace(this.SecondaryKey))
            {
                throw new Exception("PrimaryKey or SecondaryKey cannot be null.");
            }

            Context.Logger.Info("PrimaryKey = {0} SecondaryKey = {1}", this.PrimaryKey, this.SecondaryKey);

            Context.Logger.Info("primary.key = {0}",
                Context.Config.stormConf.ContainsKey("primary.key") ? Context.Config.stormConf["primary.key"] : "not found");

            Context.Logger.Info("secondary.key = {0}",
                Context.Config.stormConf.ContainsKey("secondary.key") ? Context.Config.stormConf["secondary.key"] : "not found");

            globalstopwatch = new Stopwatch();
            globalstopwatch.Start();

            emitstopwatch = new Stopwatch();
            emitstopwatch.Start();
        }

        public virtual void Execute(SCPTuple tuple)
        {
            last_receive_count++;
            global_receive_count++;

            var inputeventdata = (string)tuple.GetValue(0);
            
            try
            {
                if (inputeventdata != null)
                {
                    JToken token = JObject.Parse(inputeventdata);

                    //This assumes that you wish to respect the timestamp field in your tuple
                    //If you dont care on the order or timestamp of tuple, you can send a DateTime.UtcNow i.e. the receive time
                    //This will allow you to aggregate based on current time than original event time.
                    var timestampvalue = (string)token.SelectToken(this.appConfig.TimestampField);

                    var timestamp = new DateTime();
                    var result = DateTime.TryParse(timestampvalue, out timestamp);

                    //This computes an additional timestamp which is floored to your aggregation window
                    //This acts as an alternative strategy to TickTuples as this allows you to process multiple windows at same time 
                    //and events arriving slightly out of order. For events that are huge apart i.e. 
                    //do not even fit in multiple AggregationWindows can potentially overwrite your previous aggregations 
                    //if you dont handle it properly later in your topology by doing right merges.
                    //Based on your topology, you can choose which strategy suits you better
                    var aggregationTimestamp = timestamp.Floor(this.appConfig.AggregationWindow);

                    var primarykeyvalue = ((string)token.SelectToken(this.PrimaryKey)) ?? Utilities.DEFAULT_VALUE;
                    var secondarykeyvalue = ((string)token.SelectToken(this.SecondaryKey)) ?? Utilities.DEFAULT_VALUE;

                    //Aggregate the current input. The final argument can actually be a value of any field in your input, 
                    //allowing you to use this aggregation as sum than count.
                    //We emit the aggregated tuples as part of aggregation process and expiry of the window.
                    Aggregate(aggregationTimestamp, primarykeyvalue, secondarykeyvalue, 1);

                    //Ack the tuple to the spout so that the spout can move forward and remove the tuple from its cache.
                    //This is mandatory requirement if you use the default constructor for EventHubSpout as it uses the ack based PartitionManager
                    this.context.Ack(tuple);
                }
            }
            catch (Exception ex)
            {
                global_error_count++;
                Context.Logger.Error(ex.ToString());

                //Fail the tuple in spout if you were not able to deserialize or emit it.
                this.context.Fail(tuple);
            }
        }

        public virtual void Aggregate(DateTime aggregationTimestamp, string primarykeyvalue, string secondarykeyvalue, double value)
        {
            current_cache_size++;
            if (aggregatedCounts.ContainsKey(aggregationTimestamp))
            {
                if (aggregatedCounts[aggregationTimestamp].ContainsKey(primarykeyvalue))
                {
                    if (aggregatedCounts[aggregationTimestamp][primarykeyvalue].ContainsKey(secondarykeyvalue))
                    {
                        aggregatedCounts[aggregationTimestamp][primarykeyvalue][secondarykeyvalue] += value;
                        current_cache_size--;
                    }
                    else
                    {
                        aggregatedCounts[aggregationTimestamp][primarykeyvalue].Add(secondarykeyvalue, value);
                    }
                }
                else
                {
                    var newsecondaryaggregation = new Dictionary<string, double>();
                    newsecondaryaggregation.Add(secondarykeyvalue, value);
                    aggregatedCounts[aggregationTimestamp].Add(primarykeyvalue, newsecondaryaggregation);
                }
            }
            else
            {
                var newsecondaryaggregation = new Dictionary<string, double>();
                newsecondaryaggregation.Add(secondarykeyvalue, value);
                var newprimaryaggregation = new Dictionary<string, Dictionary<string, double>>();
                newprimaryaggregation.Add(primarykeyvalue, newsecondaryaggregation);
                aggregatedCounts.Add(aggregationTimestamp, newprimaryaggregation);
            }

            if (EmitAggregations())
            {
                Context.Logger.Info("Events Summary (Last Window): " +
                    "Received = " + last_receive_count + " (" + last_receive_count / last_emit_in_secs + " events/sec)" +
                    ", Emitted = " + last_emit_count + " (" + last_emit_count / last_emit_in_secs + " events/sec)" +
                    ", Errors = " + last_error_count);

                last_receive_count = 0;
                last_emit_count = 0;
                last_error_count = 0;
            }

            if (global_receive_count % 5000 == 0)
            {
                Context.Logger.Info("Current Cache Size = {0}, Last received event aggregation timestamp = {1}", 
                    current_cache_size, aggregationTimestamp);

                var global_emit_in_secs = globalstopwatch.Elapsed.TotalSeconds;
                Context.Logger.Info("Events Summary (Global): " +
                    "Received = " + global_receive_count + " (" + global_receive_count / global_emit_in_secs + " events/sec)" +
                    ", Emitted = " + global_emit_count + " (" + global_emit_count / global_emit_in_secs + " events/sec)" +
                    ", Errors = " + global_error_count);

                Context.Logger.Info("Process Info: " +
                "WorkingSet64 = " + Process.GetCurrentProcess().WorkingSet64 +
                ", PeakWorkingSet64 = " + Process.GetCurrentProcess().PeakWorkingSet64 +
                ", PrivateMemorySize64 = " + Process.GetCurrentProcess().PrivateMemorySize64
                );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual bool EmitAggregations()
        {
            return EmitAggregations(-1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topN"></param>
        /// <returns></returns>
        public virtual bool EmitAggregations(int topN)
        {
            try
            {
                if (emitstopwatch.Elapsed > this.appConfig.AggregationWindow)
                {
                    var emitime = DateTime.UtcNow.Floor(this.appConfig.AggregationWindow).Subtract(this.appConfig.EmitWindow);
                    var keystoremove = new List<DateTime>();
                    foreach (var dtkey in aggregatedCounts.Keys)
                    {
                        if (dtkey < emitime)
                        {
                            foreach (var pkey in aggregatedCounts[dtkey].Keys)
                            {
                                if (topN > 0)
                                {
                                    var rows = aggregatedCounts[dtkey][pkey].OrderByDescending(r => r.Value).Take(topN).ToList();
                                    foreach (var row in rows)
                                    {
                                        this.context.Emit(Constants.DEFAULT_STREAM_ID, new Values(dtkey, pkey, row.Key, row.Value));
                                        if (global_emit_count % 1000 == 0)
                                        {
                                            Context.Logger.Info("Last aggregation tuple emitted: DateTime = {0} PrimaryKey = {1} SecondaryKey = {2} AggregatedValue = {3}", dtkey, pkey, row.Key, row.Value);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var skey in aggregatedCounts[dtkey][pkey].Keys)
                                    {
                                        this.context.Emit(Constants.DEFAULT_STREAM_ID, new Values(dtkey, pkey, skey, aggregatedCounts[dtkey][pkey][skey]));
                                        if (global_emit_count % 1000 == 0)
                                        {
                                            Context.Logger.Info("Last aggregation tuple emitted: DateTime = {0} PrimaryKey = {1} SecondaryKey = {2} AggregatedValue = {3}", dtkey, pkey, skey, aggregatedCounts[dtkey][pkey][skey]);
                                        }
                                    }
                                }
                                global_emit_count += aggregatedCounts[dtkey][pkey].Count;
                                last_emit_count += aggregatedCounts[dtkey][pkey].Count;
                                current_cache_size -= aggregatedCounts[dtkey][pkey].Count;
                            }
                            keystoremove.Add(dtkey);
                        }
                    }

                    foreach (var key in keystoremove)
                    {
                        aggregatedCounts.Remove(key);
                    }
                    last_emit_in_secs = emitstopwatch.Elapsed.TotalSeconds;
                    emitstopwatch.Restart();
                }

                if (last_emit_count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                last_error_count++;
                global_error_count++;
                Context.Logger.Error(ex.ToString());
                return false;
            }
        }

        public static EventAggregator Get(Context context, Dictionary<string, Object> parms)
        {
            return new EventAggregator(context, parms);
        }

        public static List<Type> AggregatedTupleFields = new List<Type>()
        {
            typeof(DateTime), //AggregationTimestamp
            typeof(string), //PrimaryKey
            typeof(string), //SecondaryKey
            typeof(double) //AggregatedValue
        };
    }
}
