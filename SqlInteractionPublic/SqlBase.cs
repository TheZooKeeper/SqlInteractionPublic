using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace SqlInteraction
{
    /// <summary>
    /// Base class for SQL. Used to clean up SQL class
    /// </summary>
    public class SqlBase : IDisposable
    {
        public SqlConnection SQLCONN;
        protected DB_CATALOG catalog;
        protected string catalogString;
        private bool disposed;

        // TODO: Add your Databases here
        // The enum system forces use of set DB names
        public enum DB_CATALOG
        {
            TEST1,
            TEST2
        }

        protected SqlBase(DB_CATALOG dbCatalog)
        {
            SQLCONN = new SqlConnection();
            this.catalog = dbCatalog;
        }

        /// <summary>
        /// Opens DB
        /// </summary>
        /// <returns>bool as to whether DB was opened</returns>
        protected bool OpenDB()
        {
            // If for some reason the connection is open, lets close it and reopen it since the client may have changed their configuration settings
            if (SQLCONN?.State == ConnectionState.Open)
            {
                SQLCONN.Close();
                SQLCONN.Dispose();
            }

            // TODO: Update switch with catalogues
            switch (catalog)
            {
                case DB_CATALOG.TEST1:
                    catalogString = "TEST1";
                    break;

                case DB_CATALOG.TEST2:
                    catalogString = "TEST2";
                    break;
            }
            // TODO: Fill in connection string with server.
            // May need to modify connection string with institutions security settings
            SQLCONN.ConnectionString = $"packet size=4096;integrated security=SSPI;data source='{ "YOUR_SERVER_HERE" }';persist security info=False;initial catalog={catalogString}";

            try
            {
                SQLCONN.Open();
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"There was an error connecting to the {catalogString} database, error: " + ex.Message);
                return false;
            }

            if (SQLCONN.State != ConnectionState.Open)
            {
                Console.WriteLine($"There was an error connecting to the {catalogString} database.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Used to print DB Errors
        /// </summary>
        /// <param name="Message"></param>
        protected static void DBError(string Message)
        {
            Console.WriteLine(Message);
        }

        #region Procedures to run the condensed SQL Functions

        /// <summary>
        /// Returns A Scalar From Stored Procedure
        /// </summary>
        /// <param name="commandName">Name of Stored Procedure Command</param>
        /// <param name="paramList"></param>
        /// <exception cref="T:System.InvalidCastException"></exception>
        /// <exception cref="T:System.Data.SqlClient.SqlException"></exception>
        /// <exception cref="T:System.InvalidOperationException"></exception>
        /// <exception cref="T:System.IO.IOException"></exception>
        /// <exception cref="T:System.ObjectDisposedException"></exception>
        protected object ExecuteScalar(string commandName, Dictionary<string, object> paramList = null)
        {
            object retval = null;
            try
            {
                if (OpenDB())
                {
                    SqlCommand dbCM = BuildStoredProcedureCommand(commandName, paramList);
                    retval = dbCM.ExecuteScalar();
                    dbCM.Dispose();
                }
            }
            finally
            {
                CloseDB();
            }
            return retval;
        }

        /// <summary>
        /// Executes A Non Query From Stored Procedure
        /// </summary>
        /// <param name="commandName">Name of Stored Procedure Command</param>
        /// <param name="paramList"></param>
        /// <exception cref="T:System.InvalidCastException"></exception>
        /// <exception cref="T:System.Data.SqlClient.SqlException"></exception>
        /// <exception cref="T:System.InvalidOperationException"></exception>
        /// <exception cref="T:System.IO.IOException"></exception>
        /// <exception cref="T:System.ObjectDisposedException"></exception>
        protected void ExecuteNonQuery(string commandName, Dictionary<string, object> paramList = null)
        {
            try
            {
                if (OpenDB())
                {
                    SqlCommand dbCM = BuildStoredProcedureCommand(commandName, paramList);
                    dbCM.ExecuteNonQuery();
                    dbCM.Dispose();
                }
            }
            finally
            {
                CloseDB();
            }
        }

        /// <summary>
        /// Fills a Datatable From Stored Procedure
        /// </summary>
        /// <param name="commandName">Name of Stored Procedure Command</param>
        /// <param name="paramList"></param>
        /// <exception cref="T:System.InvalidOperationException"></exception>
        protected DataTable FillDataTable(string commandName, Dictionary<string, object> paramList = null)
        {
            DataTable dt = new DataTable();
            try
            {
                if (OpenDB())
                {
                    SqlCommand dbCM = BuildStoredProcedureCommand(commandName, paramList);
                    SqlDataAdapter sa = new SqlDataAdapter
                    {
                        SelectCommand = dbCM
                    };
                    sa.Fill(dt);
                    dbCM.Dispose();
                    sa.Dispose();
                }
            }
            finally
            {
                CloseDB();
            }
            return dt;
        }

        private SqlCommand BuildStoredProcedureCommand(string commandName, Dictionary<string, object> paramList)
        {
            // We are forcing the command as a stored procedure
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            SqlCommand dbCM = new SqlCommand(commandName, SQLCONN)
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            {
                CommandType = CommandType.StoredProcedure
            };

            if (paramList != null)
            {
                SqlCommandBuilder.DeriveParameters(dbCM);
                foreach (string key in paramList.Keys)
                    dbCM.Parameters[key].Value = paramList[key];
            }
            return dbCM;
        }

        #endregion Procedures to run the condensed SQL Functions

        /// <summary>
        /// Tests whether a DB connection can be opened
        /// </summary>
        /// <returns>bool as to whether connection was opened</returns>
        public bool TestDBConnection()
        {
            bool test = OpenDB();
            CloseDB();
            return test;
        }

        public bool CloseDB()
        {
            if (SQLCONN.State == ConnectionState.Open)
                SQLCONN.Close();
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
                SQLCONN.Dispose();

            disposed = true;
        }

        // This is implemented correctly, known issue in C# and vb.net
        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
#pragma warning disable CA1063 // Implement IDisposable Correctly

        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            CloseDB();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SqlBase()
        {
            Dispose(false);
        }
    }
}