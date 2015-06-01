using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AzureDocumentDBWriterStormApplication
{
    /// <summary>
    /// A SCP.Net C# Spout that emits random Vehicle records
    /// For the sake of replayability it generates the VINs in same sequence
    /// You can configure the maximum number of Vehicle records each task should emit
    /// </summary>
    public class VehicleRecordGeneratorSpoutForDocumentDB : ISCPSpout
    {
        Context context;
        long seqId = 0;

        Dictionary<long, object> cachedTuples = new Dictionary<long, object>();
        bool enableAck = false;

        long emitCount = 0;
        public static long FINAL_EMIT_COUNT = 1000;

        public static Random random = new Random();

        public VehicleRecordGeneratorSpoutForDocumentDB(Context context, Dictionary<string, object> parms = null)
        {
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the outgoing tuples for the upstream bolt tasks
            //As this is a spout, you can set inputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, OutputFieldTypes);

            this.context.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));

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
        public static VehicleRecordGeneratorSpoutForDocumentDB Get(Context context, Dictionary<string, Object> parms)
        {
            return new VehicleRecordGeneratorSpoutForDocumentDB(context, parms);
        }

        //Handy list to provide names to fields when building the topology in TopologyBuilder
        public static List<string> OutputFields = new List<string>() { "Vehicle" };

        //Handy list to provide types of fields for other tasks that will consume tuples from this spout
        //NOTE: Make sure the class is marked with Serializable attribute 
        //      if you wish to emit objects of non-primitive types like Vehilce in this case
        //Sending object in its native type is useful in a DocumentDB writing scenario
        public static List<Type> OutputFieldTypes = new List<Type>() { typeof(Vehicle) };

        /// <summary>
        /// The NextTuple method of a spout
        /// </summary>
        /// <param name="parms"></param>
        public void NextTuple(Dictionary<string, object> parms)
        {
            if (emitCount <= FINAL_EMIT_COUNT)
            {
                List<object> emitValue = new Values(Vehicle.GetRandomVehicle(emitCount));

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

    
}