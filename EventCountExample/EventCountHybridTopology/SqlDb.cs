using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventCountHybridTopology
{
    public class SqlDb
    {
        private SqlConnection con = null;
        private SqlCommand comm = null;

        public SqlDb(String connectionStr)
        {
            try
            {
                // Establish the connection.
                Context.Logger.Info("connectionStr: " + connectionStr);
                con = new SqlConnection(connectionStr);
                con.Open();
            }
            catch (Exception e)
            {
                Context.Logger.Error("Exception: {0}", e);
            }
        }

        public void createTable()
        {
            Context.Logger.Info("createTable");
            String SQL = "CREATE TABLE EHEventCountHybrid(timestamp bigint PRIMARY KEY, eventCount bigint)";

            // Create and execute an SQL statement that returns some data.
            try
            {
                comm = con.CreateCommand();
                comm.CommandText = SQL;
                comm.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Context.Logger.Error("Exception: {0}", e);
            }
        }

        public void dropTable()
        {
            Context.Logger.Info("dropTable");
            String SQL = "IF OBJECT_ID (N'dbo.EHEventCountHybrid', N'U') IS NOT NULL DROP TABLE dbo.EHEventCountHybrid";
            try
            {
                comm = con.CreateCommand();
                comm.CommandText = SQL;
                comm.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Context.Logger.Error("Exception: {0}", e);
            }
        }

        public void insertValue(long timestamp, long count)
        {
            String SQL = "INSERT INTO EHEventCountHybrid "
                + "VALUES (" + timestamp + "," + count + ");";
            try
            {
                comm = con.CreateCommand();
                comm.CommandText = SQL;
                comm.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Context.Logger.Error("Exception: {0}", e);
            }
        }

        public void resetTable()
        {
            Context.Logger.Info("resetTable");
            String SQL = "Truncate Table EHEventCountHybrid";
            try
            {
                comm = con.CreateCommand();
                comm.CommandText = SQL;
                comm.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Context.Logger.Error("Exception: {0}", e);
            }
        }
    }
}
