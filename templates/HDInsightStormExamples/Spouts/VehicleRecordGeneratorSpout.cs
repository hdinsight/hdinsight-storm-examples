using Microsoft.SCP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HDInsightStormExamples.Spouts
{
    /// <summary>
    /// A SCP.Net C# Spout that emits random Vehicle records
    /// For the sake of replayability it generates the VINs in same sequence
    /// You can configure the maximum number of Vehicle records each task should emit
    /// </summary>
    class VehicleRecordGeneratorSpout : ISCPSpout
    {
        Context context;
        long seqId = 0;

        Dictionary<long, object> cachedTuples = new Dictionary<long, object>();
        bool enableAck = false;

        long emitCount = 0;
        static long FINAL_EMIT_COUNT = 1000;

        public static Random random = new Random();

        public VehicleRecordGeneratorSpout(Context context, Dictionary<string, object> parms = null)
        {
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the outgoing tuples for the upstream bolt tasks
            //As this is a spout, you can set inputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, OutputFieldTypes);

            this.context.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));

            //TODO: Uncomment if using in reverse hybrid mode (C# -> Java) - We need to Serialize C# objects into Java objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaDeserializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer" } )
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedSerializer(new CustomizedInteropJSONSerializer());

            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);
        }

        /// <summary>
        /// VehicleRecordGeneratorSpout contructor delegate that SCP.Net uses to invoke the instance of this class
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parms"></param>
        /// <returns>Instance of VehicleRecordGeneratorSpout</returns>
        public static VehicleRecordGeneratorSpout Get(Context context, Dictionary<string, Object> parms)
        {
            return new VehicleRecordGeneratorSpout(context, parms);
        }

        //Handy list to provide types of fields for other tasks that will consume tuples from this spout
        //NOTE: Make sure the class is marked with Serializable attribute if you wish to emit objects of non-primitive typles
        
        bool emitAsValues = true; //Set this to true if you wish to use output like below else set to false if you wish to send Vehicle objects
        public static List<string> OutputFields = new List<string>() { "VIN", "Timestamp", "Make", "Model", "Year", "Status" };
        public static List<Type> OutputFieldTypes = new List<Type>() { typeof(string), typeof(DateTime), typeof(string), typeof(int), typeof(int), typeof(string) };
        //Alternate method of one field containing a complex type. Used if emitAsValues = false
        //public static List<string> OutputFields = new List<string>() { "Vehicle" };
        //public static List<Type> OutputFieldTypes = new List<Type>() { typeof(Vehicle) };

        /// <summary>
        /// The NextTuple method of a spout
        /// </summary>
        /// <param name="parms"></param>
        public void NextTuple(Dictionary<string, object> parms)
        {
            if (emitCount <= FINAL_EMIT_COUNT)
            {
                List<object> emitValue = null;
                if (emitAsValues)
                {
                    emitValue = Vehicle.GetRandomVehicle(emitCount).GetValues();
                }
                else
                {
                    emitValue = new Values(Vehicle.GetRandomVehicle(emitCount));
                }

                if (enableAck)
                {
                    //Add to the spout cache so that the tuple can be re-emitted on fail
                    cachedTuples.Add(seqId, emitValue);
                    this.context.Emit(Constants.DEFAULT_STREAM_ID, emitValue, seqId);
                    seqId++;
                }
                else
                {
                    this.context.Emit(Constants.DEFAULT_STREAM_ID, emitValue);
                }
                emitCount++;
                Context.Logger.Info("Tuples emitted: {0}, last emitted tuple: {1}", emitCount, emitValue);
            }
            else
            {
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// The ack method of a spout
        /// </summary>
        /// <param name="seqId">The sequence id of the tuple</param>
        /// <param name="parms"></param>
        public void Ack(long seqId, Dictionary<string, object> parms)
        {
            if (enableAck)
            {
                //Remove the successfully acked tuple from the cache.
                cachedTuples.Remove(seqId);
            }
        }

        /// <summary>
        /// The fail method of a spout
        /// </summary>
        /// <param name="seqId">The sequence id of the tuple</param>
        /// <param name="parms"></param>
        public void Fail(long seqId, Dictionary<string, object> parms)
        {
            if (enableAck)
            {
                //Re-emit the failed tuple again - only if it exists
                if (cachedTuples.ContainsKey(seqId))
                {
                    this.context.Emit(Constants.DEFAULT_STREAM_ID, new Values(cachedTuples[seqId]), seqId);
                }
            }
        }
    }

    /// <summary>
    /// A serializable class that represents a vehicle record
    /// </summary>
    [Serializable]
    public class Vehicle
    {
        public DateTime Timestamp { get; set; }
        public string VIN { get; set; }
        public string Make { get; set; }
        public int Model { get; set; }
        public int Year { get; set; }
        public string Status { get; set; }

        static Random random = new Random();
        static List<string> RandomMakes = new List<string>() { "AUDI", "BMW", "HONDA", "NISSAN", "TOYOTA" };
        static List<int> RandomModels = new List<int>() { 111, 222, 333, 444, 555 };
        static List<int> RandomYears = new List<int>() { 2010, 2011, 2012, 2013, 2015 };
        static List<string> RandomStatuses = new List<string>() { "PERFECT", "GOOD", "BAD", "REPAIR" };

        /// <summary>
        /// Generate a random VIN for the Vehicle
        /// </summary>
        /// <param name="seqId">seqId ensures that we always emit same VINs in same sequence</param>
        /// <returns></returns>
        static string GetRandomVIN(long seqId)
        {
            var vin = new StringBuilder();
            for (int i = 0; i < 17; i++)
            {
                vin.Append(seqId);
            }
            return vin.ToString().Substring(0, 17);
        }

        public static Vehicle GetRandomVehicle(long seqId)
        {
            var vehicle = new Vehicle()
            {
                Timestamp = DateTime.UtcNow,
                VIN = GetRandomVIN(seqId),
                Make = RandomMakes[random.Next(RandomMakes.Count)],
                Model = RandomModels[random.Next(RandomModels.Count)],
                Year = RandomYears[random.Next(RandomYears.Count)],
                Status = RandomStatuses[random.Next(RandomStatuses.Count)]
            };
            return vehicle;
        }

        public List<object> GetValues()
        {
            return new List<object>()
            {
                this.VIN, //Keep VIN as the first item in list to be used as ID in upstream bolts like HBase
                this.Timestamp,
                this.Make,
                this.Model,
                this.Year,
                this.Status
            };
        }
    }
}