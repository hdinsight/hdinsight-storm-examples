using System;
using System.Collections.Generic;
using Microsoft.SCP;

namespace Scp.App.HelloWorldHostModeMultiSpout
{
    /// <summary>
    /// The Non-Tx bolt "displayer" print the Person info to logs.
    /// </summary>
    public class Displayer : ISCPBolt
    {
        private Context ctx;

        public Displayer(Context ctx)
        {
            Context.Logger.Info("Counter constructor called");

            this.ctx = ctx;

            // Declare Input schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(SentenceGenerator.STREAM_ID, new List<Type>() { typeof(string) });
            inputSchema.Add(PersonGenerator.STREAM_ID, new List<Type>() { typeof(Person) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));
        }

        /// <summary>
        /// The Execute() function will be called, when a new tuple is available.
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            Context.Logger.Info("Execute enter");
            string streamId = tuple.GetSourceStreamId();
            switch (streamId)
            {
                case SentenceGenerator.STREAM_ID:
                    {
                        string sentence = tuple.GetString(0);
                        Context.Logger.Info("sentence: {0}", sentence);
                    }
                    break;
                case PersonGenerator.STREAM_ID:
                    {
                        Person person = (Person)tuple.GetValue(0);
                        Context.Logger.Info("person: {0}", person.ToString());
                    }
                    break;
                default:
                    Context.Logger.Info("Get unknown tuple from unknown stream.");
                    break;
            }
            Context.Logger.Info("Execute exit");

        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt</param>
        /// <returns></returns>
        public static Displayer Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new Displayer(ctx);
        }
    }
}