using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace EventCountHybridTopology
{
    /// <summary>
    /// Globally count number of messages
    /// </summary>
    public class DBGlobalCountBolt : ISCPBolt
    {
        Context ctx;

        SqlDb db;
        SqlConnectionStringBuilder sqlConnStringBuilder;

        long partialCount = 0L;
        long totalCount = 0L;

        public DBGlobalCountBolt(Context ctx)
        {
            this.ctx = ctx;

            // set input schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(long) });

            //Add the Tick tuple Stream in input streams - A tick tuple has only one field of type long
            inputSchema.Add(Constants.SYSTEM_TICK_STREAM_ID, new List<Type>() { typeof(long) });

            // Declare input and output schemas
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            sqlConnStringBuilder = new SqlConnectionStringBuilder();
            sqlConnStringBuilder.DataSource = ConfigurationManager.AppSettings["SqlDbServerName"] + ".database.windows.net";
            sqlConnStringBuilder.InitialCatalog = ConfigurationManager.AppSettings["SqlDbDatabaseName"];
            sqlConnStringBuilder.UserID = ConfigurationManager.AppSettings["SqlDbUsername"];
            sqlConnStringBuilder.Password = ConfigurationManager.AppSettings["SqlDbPassword"];

            db = new SqlDb(sqlConnStringBuilder.ConnectionString);
            db.dropTable();
            db.createTable();

            partialCount = 0L;
            totalCount = 0L;
        }

        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        /// <summary>
        /// The Execute() function will be called, when a new tuple is available.
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            if (tuple.GetSourceStreamId().Equals(Constants.SYSTEM_TICK_STREAM_ID))
            {
                if (partialCount > 0)
                {
                    Context.Logger.Info("updating database" +
                        ", partialCount: " + partialCount +
                        ", totalCount: " + totalCount);
                    db.insertValue(CurrentTimeMillis(), partialCount);
                    partialCount = 0L;
                }
            }
            else
            {
                //Merge partialCount from all PartialCountBolt tasks
                var incomingPartialCount = tuple.GetLong(0);
                partialCount += incomingPartialCount;
                totalCount += incomingPartialCount;
            }
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt</param>
        /// <returns></returns>
        public static DBGlobalCountBolt Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new DBGlobalCountBolt(ctx);
        }
    }
}