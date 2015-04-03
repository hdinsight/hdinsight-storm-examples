using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.SCP;
using Tweetinvi;
using System.Configuration;
using Tweetinvi.Core.Interfaces;
using Newtonsoft.Json;

namespace HDInsightStormExamples.Spouts
{
    /// <summary>
    /// A SCP.Net C# Spout that listens to twitter feed and emits the tweet data
    /// </summary>
    public class TwitterSampleSpout : ISCPSpout
    {
        Context context;
        Queue<ITweet> queue = new Queue<ITweet>();

        long seqId = 0;
        Dictionary<long, ITweet> cache = new Dictionary<long, ITweet>();

        public TwitterSampleSpout(Context context)
        {
            Context.Logger.Info(this.GetType().Name + " constructor called");
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the outgoing tuples for the upstream bolt tasks
            //As this is a spout, you can set inputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });
            this.context.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));

            //TODO: Specify your twitter credentials in App.Config
            TwitterCredentials.SetCredentials(
                ConfigurationManager.AppSettings["TwitterAccessToken"],
                ConfigurationManager.AppSettings["TwitterAccessTokenSecret"], 
                ConfigurationManager.AppSettings["TwitterConsumerKey"], 
                ConfigurationManager.AppSettings["TwitterConsumerSecret"]);

            //Setup a Twitter Stream as per your requirements
            CreateSampleStream();
            //CreateFilteredStream();
        }

        public void CreateSampleStream()
        {
            var stream = Tweetinvi.Stream.CreateSampleStream();
            stream.TweetReceived += (sender, args) => { GetTweet(args.Tweet); };
            stream.StartStreamAsync();
        }

        public void CreateFilteredStream()
        {
            var stream = Tweetinvi.Stream.CreateFilteredStream();
            stream.MatchingTweetReceived += (sender, args) => { GetTweet(args.Tweet); };

            //TODO: Setup your filter criteria
            stream.AddTrack("Microsoft");
            stream.AddTrack("HDInsight");
            stream.AddTrack("Storm");
            stream.AddTrack("SCP.Net");
            stream.StartStreamMatchingAnyConditionAsync();
        }

        public static TwitterSampleSpout Get(Context context, Dictionary<string, Object> parms)
        {
            return new TwitterSampleSpout(context);
        }

        /// <summary>
        /// The twitter async stream methods call this method to queue the tweets
        /// </summary>
        /// <param name="tweet"></param>
        public void GetTweet(ITweet tweet)
        {
            queue.Enqueue(tweet);
        }

        public void NextTuple(Dictionary<string, Object> parms)
        {
            if (queue.Count > 0)
            {
                var tweet = queue.Dequeue();
                cache.Add(seqId++, tweet);
                context.Emit(Constants.DEFAULT_STREAM_ID, new Values(JsonConvert.SerializeObject(tweet)), seqId);
                Context.Logger.Info("NextTuple: Emitted Tweet = {0}", tweet.Text);
            }
            else
            {
                //Free up some CPU cycles if no tweets are being received
                Thread.Sleep(50);
            }
        }

        public void Ack(long seqId, Dictionary<string, Object> parms)
        {
            cache.Remove(seqId);
        }

        public void Fail(long seqId, Dictionary<string, Object> parms)
        {
            this.context.Emit(new Values(JsonConvert.SerializeObject(cache[seqId])));
        }
    }
}