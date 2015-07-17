using System;
using System.Collections.Generic;
using Microsoft.SCP;

namespace Scp.App.HelloWorldHostModeMultiSpout
{
    /// <summary>
    /// Example of transport Class Object between C# and Java 
    /// </summary>
    [Serializable]
    public class Person
    {
        public string name;
        public int age;

        public Person(string name, int age)
        {
            this.name = name;
            this.age = age;
        }

        public override string ToString()
        {
            return string.Format("(name: {0}, age: {1})", name, age);
        }
    }

    /// <summary>
    /// The Non-Tx spout "generator" will randomly emit Person infomations to "displayer". 
    /// </summary>
    public class PersonGenerator : ISCPSpout
    {
        public const string STREAM_ID = "PersonStream";

        private Context ctx;
        private Random rand = new Random();
        private Person[] persons = new Person[]
        {
            new Person("Tom", 20),
            new Person("Marry", 18), 
            new Person("David", 25)
        };

        public PersonGenerator(Context ctx)
        {
            Context.Logger.Info("SentenceGenerator constructor called");
            this.ctx = ctx;

            // Declare Output schema
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(STREAM_ID, new List<Type>() { typeof(Person) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));
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

            Person person = persons[rand.Next(0, persons.Length)];
            Context.Logger.Info("Emit: {0}", person.ToString());
            this.ctx.Emit(STREAM_ID, new Values(person));
            Context.Logger.Info("NextTuple exit");
        }

        /// <summary>
        /// Ack() will be called only when ack mechanism is enabled in spec file.
        /// If ack is not supported in non-transactional topology, the Ack() can be left as empty function. 
        /// </summary>
        /// <param name="seqId">Sequence Id of the tuple which is acked.</param>
        /// <param name="parms"></param>
        public void Ack(long seqId, Dictionary<string, Object> parms)
        {
        }

        /// <summary>
        /// Fail() will be called only when ack mechanism is enabled in spec file. 
        /// If ack is not supported in non-transactional topology, the Fail() can be left as empty function.
        /// </summary>
        /// <param name="seqId">Sequence Id of the tuple which is failed.</param>
        /// <param name="parms"></param>
        public void Fail(long seqId, Dictionary<string, Object> parms)
        {
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt</param>
        /// <returns></returns>
        public static PersonGenerator Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new PersonGenerator(ctx);
        }
    }
}