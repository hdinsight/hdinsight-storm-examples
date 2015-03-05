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
    /// This Top N Ranker class is inherited from EventReAggregator
    /// This will only emit Top N records of aggregations so far
    /// </summary>
    class EventRanker : EventReAggregator
    {
        public EventRanker()
        {
        }

        public EventRanker(Context context, Dictionary<string, Object> parms = null)
        {
            this.context = context;

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, AggregatedTupleFields);

            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, AggregatedTupleFields);

            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));

            Initialize(parms);

            Context.Logger.Info("AggregationRankerTopNCount = " + this.appConfig.AggregationRankerTopNCount);
        }

        /// <summary>
        /// This method just calls the ProcessTuple method from its base class
        /// </summary>
        /// <param name="tuple"></param>
        public override void Execute(SCPTuple tuple)
        {
            ProcessTuple(tuple);
        }

        /// <summary>
        /// This method overrides the EmitAggregations method to only emit Top N count
        /// </summary>
        /// <returns></returns>
        public override bool EmitAggregations()
        {
            return EmitAggregations(this.appConfig.AggregationRankerTopNCount);
        }

        public new static EventRanker Get(Context context, Dictionary<string, Object> parms)
        {
            return new EventRanker(context, parms);
        }
    }
}
