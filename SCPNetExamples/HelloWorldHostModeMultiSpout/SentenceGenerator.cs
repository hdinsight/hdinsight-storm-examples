using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using Microsoft.SCP;

namespace Scp.App.HelloWorldHostModeMultiSpout
{
    /// <summary>
    /// The spout "generator" will randomly generate some sentences, and emit these sentences to "splitter". 
    /// </summary>
    public class SentenceGenerator : ISCPSpout
    {
        public const string STREAM_ID = "SentenceStream";

        private const int MAX_PENDING_TUPLE_NUM = 10;

        private Context ctx;
        private Configuration cfg;

        private bool enableAck = false;
        private long lastSeqId = 0;
        private Dictionary<long, string> cachedTuples = new Dictionary<long, string>();

        private Random rand = new Random();
        string[] sentences = new string[] {
                                          "the cow jumped over the moon",
                                          "an apple a day keeps the doctor away",
                                          "four score and seven years ago",
                                          "snow white and the seven dwarfs",
                                          "i am at two with nature"};

        public SentenceGenerator(Context ctx, Dictionary<string, Object> parms = null)
        {
            Context.Logger.Info("SentenceGenerator constructor called");
            this.ctx = ctx;

            // Demo how to get User customized config from parms 
            if (parms != null && parms.ContainsKey(Constants.USER_CONFIG))
            {
                this.cfg = (Configuration)parms[Constants.USER_CONFIG];
            }
            
            if (cfg != null)
            {
                Context.Logger.Info(string.Format("New \"SentenceGenerator\" instance created with config setting: {0}={1}.", "BatchSize", cfg.AppSettings.Settings["BatchSize"].Value));
            }

            // Declare Output schema
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(STREAM_ID, new List<Type>() { typeof(string) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));

            // Demo how to get pluginConf info and enable ACK in Non-Tx topology
            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);

            Context.Logger.Info("Get app settings : <foo, {0}>", ConfigurationManager.AppSettings.Get("foo"));

            ConnectionStringSettingsCollection settings = ConfigurationManager.ConnectionStrings;
            if (settings != null)
            {
                foreach (ConnectionStringSettings cs in settings)
                {
                    Context.Logger.Info("Get ConnectionString: <\"{0}\", \"{1}\", \"{2}\">", cs.Name, cs.ProviderName, cs.ConnectionString);
                }
            }

        }

        /// <summary>
        /// This method is used to emit one or more tuples. If there is nothing to emit, this method should return without emitting anything. 
        /// It should be noted that NextTuple(), Ack(), and Fail() are all called in a tight loop in a single thread in C# process. 
        /// When there are no tuples to emit, it is courteous to have NextTuple sleep for a short amount of time (such as 10 milliseconds), so as not to waste too much CPU.
        /// </summary>
        /// <param name="parms"></param>
        public void NextTuple(Dictionary<string, Object> parms)
        {
            Context.Logger.Info("NextTuple enter");
            string sentence;

            if (enableAck)
            {
                if (cachedTuples.Count <= MAX_PENDING_TUPLE_NUM)
                {
                    lastSeqId++;
                    sentence = sentences[rand.Next(0, sentences.Length - 1)];
                    Context.Logger.Info("Emit: {0}, seqId: {1}", sentence, lastSeqId);
                    this.ctx.Emit(STREAM_ID, new Values(sentence), lastSeqId);
                    cachedTuples[lastSeqId] = sentence;
                }
                else
                {
                    // if have nothing to emit, then sleep for a little while to release CPU
                    Thread.Sleep(50);
                }
                Context.Logger.Info("cached tuple num: {0}", cachedTuples.Count);
            }
            else
            {
                sentence = sentences[rand.Next(0, sentences.Length - 1)];
                Context.Logger.Info("Emit: {0}", sentence);
                this.ctx.Emit(STREAM_ID, new Values(sentence));
            }

            Context.Logger.Info("NextTx exit");
        }

        /// <summary>
        /// Ack() will be called only when ack mechanism is enabled in spec file.
        /// If ack is not supported in non-transactional topology, the Ack() can be left as empty function. 
        /// </summary>
        /// <param name="seqId">Sequence Id of the tuple which is acked.</param>
        /// <param name="parms"></param>
        public void Ack(long seqId, Dictionary<string, Object> parms)
        {
            Context.Logger.Info("Ack, seqId: {0}", seqId);
            bool result = cachedTuples.Remove(seqId);
            if (!result)
            {
                Context.Logger.Warn("Ack(), remove cached tuple for seqId {0} fail!", seqId);
            }
        }

        /// <summary>
        /// Fail() will be called only when ack mechanism is enabled in spec file. 
        /// If ack is not supported in non-transactional topology, the Fail() can be left as empty function.
        /// </summary>
        /// <param name="seqId">Sequence Id of the tuple which is failed.</param>
        /// <param name="parms"></param>
        public void Fail(long seqId, Dictionary<string, Object> parms)
        {
            Context.Logger.Info("Fail, seqId: {0}", seqId);
            if (cachedTuples.ContainsKey(seqId))
            {
                string sentence = cachedTuples[seqId];
                Context.Logger.Info("Re-Emit: {0}, seqId: {1}", sentence, seqId);
                this.ctx.Emit(STREAM_ID, new Values(sentence), seqId);
            }
            else
            {
                Context.Logger.Warn("Fail(), can't find cached tuple for seqId {0}!", seqId);
            }
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt</param>
        /// <returns></returns>
        public static SentenceGenerator Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new SentenceGenerator(ctx, parms);
        }
    }
}