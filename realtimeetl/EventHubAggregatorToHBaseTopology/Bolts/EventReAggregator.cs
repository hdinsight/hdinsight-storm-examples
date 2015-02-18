using EventHubAggregatorToHBaseTopology.Common;
using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubAggregatorToHBaseTopology.Bolts
{
    /// <summary>
    /// This is a re-aggregator class that allows for us to field group from any previous aggregated class.
    /// Having good field grouping and re-aggregation help us to scale the topology and not have global bolts for final counts
    /// </summary>
    class EventReAggregator : EventAggregator
    {
        public EventReAggregator()
        {
        }

        public EventReAggregator(Context context, Dictionary<string, Object> parms = null)
        {
            this.context = context;

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, AggregatedTupleFields);

            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, AggregatedTupleFields);

            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));

            Initialize(parms);
        }

        public override void Execute(SCPTuple tuple)
        {
            ProcessTuple(tuple);
        }

        public virtual void ProcessTuple(SCPTuple tuple)
        {
            last_receive_count++;
            global_receive_count++;

            try
            {
                var aggregationTimestamp = (DateTime)tuple.GetValue(0);
                var primarykeyvalue = ((string)tuple.GetValue(1)) ?? Utilities.DEFAULT_VALUE;
                var secondarykeyvalue = ((string)tuple.GetValue(2)) ?? Utilities.DEFAULT_VALUE;
                var value = (double)tuple.GetValue(3);

                if (aggregationTimestamp != null)
                {
                    Aggregate(aggregationTimestamp, primarykeyvalue, secondarykeyvalue, value);
                }
                else
                {
                    Context.Logger.Warn("Cannot Aggregate: aggregationTimestamp is null. PrimaryKeyValue = {0}, SecondaryKeyValue = {1}, AggregationValue = {2}",
                        primarykeyvalue, secondarykeyvalue, value);
                }
            }
            catch (Exception ex)
            {
                global_error_count++;
                last_error_count++;
                Context.Logger.Error(ex.ToString());
            }
        }

        public new static EventReAggregator Get(Context context, Dictionary<string, Object> parms)
        {
            return new EventReAggregator(context, parms);
        }
    }
}
