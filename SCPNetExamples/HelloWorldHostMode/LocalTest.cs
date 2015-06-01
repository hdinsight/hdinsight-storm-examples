using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;

namespace Scp.App.HelloWorld
{
    class LocalTest
    {
        /// <summary>
        /// Here is a example to test the topology as a standalone console application.
        /// "LocalContext" is a local-mode SCP Context which is used to initialize a component,
        /// and each components communicate by using a plain text file.
        /// </summary>
        public void RunTestCase()
        {
            Dictionary<string, Object> emptyDictionary = new Dictionary<string, object>();

            {
                LocalContext generatorCtx = LocalContext.Get();
                Generator generator = Generator.Get(generatorCtx, emptyDictionary);

                for (int i = 0; i < 10; i++)
                {
                    generator.NextTuple(emptyDictionary);
                }
                generatorCtx.WriteMsgQueueToFile("generator.txt");
            }

            {
                LocalContext splitterCtx = LocalContext.Get();
                Splitter splitter = Splitter.Get(splitterCtx, emptyDictionary);

                splitterCtx.ReadFromFileToMsgQueue("generator.txt");
                List<SCPTuple> batch = splitterCtx.RecvFromMsgQueue();
                foreach (SCPTuple tuple in batch)
                {
                    splitter.Execute(tuple);
                }
                splitterCtx.WriteMsgQueueToFile("splitter.txt");
            }

            {
                LocalContext counterCtx = LocalContext.Get();
                Counter counter = Counter.Get(counterCtx, emptyDictionary);

                counterCtx.ReadFromFileToMsgQueue("splitter.txt");
                List<SCPTuple> batch = counterCtx.RecvFromMsgQueue();
                foreach (SCPTuple tuple in batch)
                {
                    counter.Execute(tuple);
                }
                counterCtx.WriteMsgQueueToFile("counter.txt");
            }
        }
    }
}
