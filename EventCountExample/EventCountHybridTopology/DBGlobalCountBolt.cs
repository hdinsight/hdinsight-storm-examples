using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;
using System.Diagnostics;
using System.Data.SqlClient;

namespace EventCountHybridTopology
{
    /// <summary>
    /// Globally count number of messages
    /// </summary>
    public class DBGlobalCountBolt : ISCPBolt
    {
        Context ctx;
        AppConfig appConfig;

        SqlDb db;
        SqlConnectionStringBuilder sqlConnStringBuilder;

        long curCountForDB = 0L;
        long totalCount = 0L;
        long finalCount = 0L;

        Stopwatch stopwatch;

        public DBGlobalCountBolt(Context ctx)
        {
            this.ctx = ctx;
            this.appConfig = new AppConfig();

            // Declare Input schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(long) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            sqlConnStringBuilder = new SqlConnectionStringBuilder();
            sqlConnStringBuilder.DataSource = appConfig.SqlDbServerName + ".database.windows.net";
            sqlConnStringBuilder.InitialCatalog = appConfig.SqlDbDatabaseName;
            sqlConnStringBuilder.UserID = appConfig.SqlDbUsername;
            sqlConnStringBuilder.Password = appConfig.SqlDbPassword;

            db = new SqlDb(sqlConnStringBuilder.ConnectionString);
            db.dropTable();
            db.createTable();

            curCountForDB = 0L;
            totalCount = 0L;
            finalCount = appConfig.EventCountPerPartition * appConfig.EventHubPartitions;

            Context.Logger.Info("finalCount: " + finalCount);
            stopwatch = Stopwatch.StartNew();
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
            //Merge partialCount from all EventCountPartialCountBolt 
            var partialCount = tuple.GetLong(0);
            curCountForDB += partialCount;
            totalCount += partialCount;

            //Write the curCountForDB every second. 
            //Specially handle the end of stream using finalCount so that this bolt flushes the count on last expected tuple
            //Alternatively you can also use TickTuple to create this 1 second rolling window
            //We chose not to use it to have a parity between the Java and SCP.Net for now
            //TODO: In future when SCP.Net will support TickTuple, we should revisit and change this back to using TickTuple
            if ((stopwatch.ElapsedMilliseconds >= 1000L) || (totalCount >= finalCount))
            {
                Context.Logger.Info("updating database" +
                    ", curCountForDB: " + curCountForDB + 
                    ", totalCount: " + totalCount +
                    ", finalCount: " + finalCount);
                db.insertValue(CurrentTimeMillis(), curCountForDB);
                curCountForDB = 0L;
                stopwatch.Restart();
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