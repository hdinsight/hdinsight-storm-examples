using HDInsightStormExamples.Spouts;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HDInsightStormExamples.Bolts
{
    /// <summary>
    /// OVERVIEW:
    /// SqlAzureWriterBolt - A bolt that can insert, upsert or delete from Sql Azure.
    /// Change or override the execute method to as per your requirements.
    /// The bolt uses parameterized T-SQL statements based on specified table schema
    /// 
    /// PRE-REQUISITES:
    /// 1. Sql Azure Server, Database and Table - All values need to be specifed in AppSettings
    ///   a. SqlAzureConnectionString - your Sql Azure server connection string
    ///   b. SqlAzureTableName - your Sql Azure table name
    ///   c. SqlAzureTableColumns - comma separated column names, required for query building
    /// 2. Sql Table Schema  
    ///   a. Note that Sql Azure requires your table schema to have a clustered index
    ///   b. This can be easily acheived by including an identity column as the primary key which gets automatically generated on each insert
    ///   c. For e.g.: Create Table #your_table# ([ID] INT IDENTITY PRIMARY KEY, ...
    ///   
    /// ASSUMPTIONS:
    /// 1. The type infering of tuple field (SqlParameter) is left to the provider as we use AddWithValue. To avoid surprises you can specify SqlDbType directly.
    /// 
    /// NUGET: 
    /// 1. SCP.Net - http://www.nuget.org/packages/Microsoft.SCP.Net.SDK/
    /// 2. Sql Transient Fault handling - http://www.nuget.org/packages/EnterpriseLibrary.TransientFaultHandling.Data
    /// 3. Newtonsoft.Json - http://www.nuget.org/packages/Newtonsoft.JSON/
    /// 
    /// REFERENCES:
    /// 1. Reliably connect to Azure SQL Database - https://msdn.microsoft.com/en-us/library/azure/dn864744.aspx
    /// </summary>
    public class SqlAzureBolt : ISCPBolt
    {
        public Context context;
        public bool enableAck = false;

        public String SqlConnectionString { get; set; }
        public ReliableSqlConnection SqlConnection { get; set; }
        public String SqlTableName { get; set; }

        public SqlCommand SqlInsertCommand { get; set; }
        public SqlCommand SqlUpsertCommand { get; set; }
        public SqlCommand SqlDeleteCommand { get; set; }

        //We need tableColumns as SCP.Net does not support Tuple.GetFields (yet)
        public List<string> SqlTableColumns { get; set; }

        RetryPolicy ConnectionRetryPolicy { get; set; }
        RetryPolicy CommandRetryPolicy { get; set; }

        public SqlAzureBolt(Context context, Dictionary<string, Object> parms)
        {
            Context.Logger.Info(this.GetType().Name + " constructor called");
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the incoming tuples from the downstream spout or bolt tasks
            //You will also need to declare the schema for the any outgoing tuples to the upstream spout or bolt tasks
            //If there are no outgoing tuples, you can set outputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            //One way is to have the OutputFieldTypes list exposed from Spout or Bolt to make it easy to tie with bolts that will consume it
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, IISLogGeneratorSpout.OutputFieldTypes);
            //Or you can declare the list directly
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(int), typeof(DateTime), typeof(string) });

            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            //TODO: Uncomment if using in hybrid mode (Java -> C#) - We need to Deserialize Java objects into C# objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaSerializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" } )
            //If the incoming tuples have only string fields, this is optional
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);

            InitializeSqlAzure();
        }

        /// <summary>
        /// A delegate method to return the instance of this class
        /// </summary>
        /// <param name="context">SCP Context, automatically passed by SCP.Net</param>
        /// <param name="parms"></param>
        /// <returns>An instance of the current class</returns>
        public static SqlAzureBolt Get(Context context, Dictionary<string, Object> parms)
        {
            return new SqlAzureBolt(context, parms);
        }

        /// <summary>
        /// Initialize the Sql Azure settings and connections
        /// </summary>
        public void InitializeSqlAzure()
        {
            this.SqlConnectionString = ConfigurationManager.AppSettings["SqlAzureConnectionString"];
            if(String.IsNullOrWhiteSpace(this.SqlConnectionString))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "SqlAzureConnectionString");
            }

            this.SqlTableName = ConfigurationManager.AppSettings["SqlAzureTableName"];
            if (String.IsNullOrWhiteSpace(this.SqlTableName))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "SqlAzureTableName");
            }

            var columns = ConfigurationManager.AppSettings["SqlAzureTableColumns"];
            if (String.IsNullOrWhiteSpace(columns))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "SqlAzureTableColumns");
            }

            this.SqlTableColumns = columns.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();

            //Reference: https://msdn.microsoft.com/en-us/library/azure/dn864744.aspx
            //1. Define an Exponential Backoff retry strategy for Azure SQL Database throttling (ExponentialBackoff Class). An exponential back-off strategy will gracefully back off the load on the service.
            int retryCount = 4;
            int minBackoffDelayMilliseconds = 2000;
            int maxBackoffDelayMilliseconds = 8000;
            int deltaBackoffMilliseconds = 2000;

            ExponentialBackoff exponentialBackoffStrategy = 
                new ExponentialBackoff("exponentialBackoffStrategy",
                    retryCount,
                    TimeSpan.FromMilliseconds(minBackoffDelayMilliseconds), 
                    TimeSpan.FromMilliseconds(maxBackoffDelayMilliseconds),
                    TimeSpan.FromMilliseconds(deltaBackoffMilliseconds));

            //2. Set a default strategy to Exponential Backoff.
            RetryManager manager = new RetryManager(
                new List<RetryStrategy>
                {  
                    exponentialBackoffStrategy 
                },
                "exponentialBackoffStrategy");

            //3. Set a default Retry Manager. A RetryManager provides retry functionality, or if you are using declarative configuration, you can invoke the RetryPolicyFactory.CreateDefault
            RetryManager.SetDefault(manager);

            //4. Define a default SQL Connection retry policy and SQL Command retry policy. A policy provides a retry mechanism for unreliable actions and transient conditions.
            ConnectionRetryPolicy = manager.GetDefaultSqlConnectionRetryPolicy();
            CommandRetryPolicy = manager.GetDefaultSqlCommandRetryPolicy();

            //5. Create a function that will retry the connection using a ReliableSqlConnection.
            InitializeSqlAzureConnection();
        }

        /// <summary>
        /// Initialize Sql Azure Connection if not open
        /// </summary>
        public void InitializeSqlAzureConnection()
        {
            try
            {
                if (this.SqlConnection == null)
                {
                    this.SqlConnection = new ReliableSqlConnection(this.SqlConnectionString, ConnectionRetryPolicy, CommandRetryPolicy);
                }

                if (this.SqlConnection.State != ConnectionState.Open)
                {
                    ConnectionRetryPolicy.ExecuteAction(() =>
                        {
                            this.SqlConnection.Open();
                        });
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                throw;
            }
        }

        /// <summary>
        /// Generates T-SQL Insert statement
        /// </summary>
        /// <returns>T-SQL Insert statement</returns>
        public string GenerateInsertTSql()
        {
            var command = new StringBuilder();
            command.AppendFormat("INSERT INTO [{0}] (", SqlTableName);

            for (int i = 0; i < this.SqlTableColumns.Count; i++)
            {
                if (i > 0)
                {
                    command.AppendFormat(", ");
                }
                command.AppendFormat(this.SqlTableColumns[i]);
            }

            command.AppendFormat(")\r\nVALUES (");

            for (int i = 0; i < this.SqlTableColumns.Count; i++)
            {
                if (i > 0)
                {
                    command.AppendFormat(", ");
                }
                command.AppendFormat("@" + this.SqlTableColumns[i]);
            }

            command.AppendFormat(")");
            Context.Logger.Info("Generated Insert Sql Command: " + command.ToString());
            return command.ToString();
        }

        /// <summary>
        /// Prepares Sql Insert Command
        /// </summary>
        public void PrepareSqlInsertCommand(List<object> row)
        {
            InitializeSqlAzureConnection();
            if (this.SqlInsertCommand == null)
            {
                this.SqlInsertCommand = this.SqlConnection.CreateCommand();
                this.SqlInsertCommand.CommandText = GenerateInsertTSql();
                for (int i = 0; i < this.SqlTableColumns.Count; i++)
                {
                    this.SqlInsertCommand.Parameters.AddWithValue("@" + this.SqlTableColumns[i], row[i]);
                }
                Context.Logger.Info("Prepared SqlInsertCommand: " + this.SqlInsertCommand.CommandText);
            }
        }

        public void Insert(List<object> row)
        {
            PrepareSqlInsertCommand(row);
            for (int i = 0; i < this.SqlTableColumns.Count; i++)
            {
                this.SqlInsertCommand.Parameters["@" + this.SqlTableColumns[i]].Value = row[i];
            }
            ExecuteCommand(this.SqlInsertCommand);
        }

        /// <summary>
        /// Instead of using TableColumns we will use the keys list
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public string GenerateWhereTSql(List<int> keys)
        {
            var command = new StringBuilder();
            if (keys.Count > 0)
            {
                command.AppendFormat(" WHERE ");
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i > 0)
                    {
                        command.AppendFormat(" and ");
                    }
                    command.AppendFormat(" [{0}] = {1} ", this.SqlTableColumns[keys[i]], "@" + this.SqlTableColumns[keys[i]]);
                }
            }
            return command.ToString();
        }

        public string GenerateUpdateTSql(List<int> keys)
        {
            var command = new StringBuilder();
            command.AppendFormat("UPDATE [{0}] set ", SqlTableName);
            for (int i = 0; i < this.SqlTableColumns.Count; i++)
            {
                if (i > 0)
                {
                    command.AppendFormat(", ");
                }
                command.AppendFormat(" [{0}] = {1}", this.SqlTableColumns[i], "@" + this.SqlTableColumns[i]);
            }

            command.AppendFormat(GenerateWhereTSql(keys));
            Context.Logger.Info("Generated Update Sql Command: " + command.ToString());
            return command.ToString();
        }

        public string GenerateUpsertTSql(List<int> keys)
        {
            var command = new StringBuilder();
            command.AppendFormat("IF EXISTS (SELECT * FROM [{0}] ", SqlTableName);
            command.AppendFormat(GenerateWhereTSql(keys));
            command.AppendFormat(")\r\nBEGIN\r\n");
            command.AppendFormat(GenerateUpdateTSql(keys));
            command.AppendFormat("\r\nEND\r\n");
            command.AppendFormat("\r\nELSE\r\n");
            command.AppendFormat("\r\nBEGIN\r\n");
            command.AppendFormat(GenerateInsertTSql());
            command.AppendFormat("\r\nEND");
            Context.Logger.Info("Generated Upsert Sql Command: " + command.ToString());
            return command.ToString();
        }

        public void PrepareSqlUpsertCommand(List<int> keys, List<object> row)
        {
            InitializeSqlAzureConnection();
            if (this.SqlUpsertCommand == null)
            {
                this.SqlUpsertCommand = this.SqlConnection.CreateCommand();
                this.SqlUpsertCommand.CommandText = GenerateUpsertTSql(keys);
                for (int i = 0; i < SqlTableColumns.Count; i++)
                {
                    this.SqlUpsertCommand.Parameters.AddWithValue("@" + this.SqlTableColumns[i], row[i]);
                }
                Context.Logger.Info("Prepared Upsert Sql Command: " + this.SqlUpsertCommand.CommandText);
            }
        }

        public void Upsert(List<int> keys, List<object> row)
        {
            PrepareSqlUpsertCommand(keys, row);
            for (int i = 0; i < this.SqlTableColumns.Count; i++)
            {
                this.SqlUpsertCommand.Parameters["@" + this.SqlTableColumns[i]].Value = row[i];
            }
            ExecuteCommand(this.SqlUpsertCommand);
        }

        /// <summary>
        /// Prepares SQL Delete command
        /// </summary>
        /// <param name="keys">Index of keys within the tuple fields which need to be deleted</param>
        public void PrepareSqlDeleteCommand(List<int> keys, List<object> row)
        {
            InitializeSqlAzureConnection();
            if (this.SqlDeleteCommand == null)
            {
                this.SqlDeleteCommand = this.SqlConnection.CreateCommand();
                this.SqlDeleteCommand.CommandText = String.Format("DELETE FROM [{0}] {1}", SqlTableName, GenerateWhereTSql(keys));
                for (int i = 0; i < keys.Count; i++)
                {
                    this.SqlDeleteCommand.Parameters.AddWithValue("@" + this.SqlTableColumns[keys[i]], row[i]);
                }
                Context.Logger.Info("Prepared SqlDeleteCommand: " + this.SqlDeleteCommand.CommandText);
            }
        }

        public void Delete(List<int> keys, List<object> row)
        {
            PrepareSqlDeleteCommand(keys, row);
            for (int i = 0; i < keys.Count; i++)
            {
                this.SqlDeleteCommand.Parameters["@" + this.SqlTableColumns[keys[i]]].Value = row[i];
            }
            ExecuteCommand(this.SqlDeleteCommand);
        }

        public void ExecuteCommand(SqlCommand sqlCommand)
        {
            try
            {
                Context.Logger.Info("Executing Command: {0}", sqlCommand.CommandText);
                var rowsAffected = this.SqlConnection.ExecuteCommand(sqlCommand, CommandRetryPolicy);
                Context.Logger.Info("RowsAffected: {0}", rowsAffected);
            }
            catch (Exception ex)
            {
                Context.Logger.Error("Exception encountered while executing command. Command: {0}", sqlCommand.CommandText);
                HandleException(ex);
                throw;
            }
        }

        public void Execute(SCPTuple tuple)
        {
            try
            {
                //TODO: Insert or Upsert or Delete depending on your logic
                //Delete(new List<int>() { 1, 2 }, tuple.GetValues());
                //Upsert(new List<int>() { 1, 2 }, tuple.GetValues());
                Insert(tuple.GetValues());

                //Ack the tuple if enableAck is set to true in TopologyBuilder. This is mandatory if the downstream bolt or spout expects an ack.
                if (enableAck)
                {
                    this.context.Ack(tuple);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Error("An error occured while executing Tuple Id: {0}. Exception Details:\r\n{1}",
                    tuple.GetTupleId(), ex.ToString());

                //Fail the tuple if enableAck is set to true in TopologyBuilder so that the tuple is replayed.
                if (enableAck)
                {
                    this.context.Fail(tuple);
                }
            }
        }

        /// <summary>
        /// Log the exception and reset any prepared commands and connections
        /// </summary>
        /// <param name="ex">The exception thrown</param>
        public void HandleException(Exception ex)
        {
            StackFrame frame = new StackFrame(1);
            MethodBase method = frame.GetMethod();
            Context.Logger.Error("{0} threw an exception. Exception Details: {1}", method.Name, ex.ToString());
            Context.Logger.Info("Resetting all commands and connections");
            if (this.SqlInsertCommand != null)
            {
                this.SqlInsertCommand.Dispose();
                this.SqlInsertCommand = null;
            }

            if (this.SqlUpsertCommand != null)
            {
                this.SqlUpsertCommand.Dispose();
                this.SqlUpsertCommand = null;
            }

            if (this.SqlDeleteCommand != null)
            {
                this.SqlDeleteCommand.Dispose();
                this.SqlDeleteCommand = null;
            }

            if (this.SqlConnection != null)
            {
                if (this.SqlConnection.State == ConnectionState.Open)
                {
                    this.SqlConnection.Close();
                }
                this.SqlConnection.Dispose();
                this.SqlConnection = null;
            }
        }
    }
}
