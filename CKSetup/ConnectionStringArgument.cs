using CK.Core;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    class ConnectionStringArgument
    {
        public ConnectionStringArgument( CommandArgument arg )
        {
            CommandArgument = arg;
        }

        public CommandArgument CommandArgument { get; }

        public string TargetConnectionString { get; private set; }

        public string MasterConnectionString { get; private set; }

        public string TargetDatabaseName { get; private set; }

        public bool Initialize( ConsoleMonitor m )
        {
            string connectionString = CommandArgument.Value;
            if( string.IsNullOrEmpty( connectionString ) )
            {
                return m.SendErrorAndDisplayHelp( "A connection string is required." ) == Program.RetCodeSuccess;
            }
            SqlConnectionStringBuilder dcsb = new SqlConnectionStringBuilder( connectionString );
            string targetDatabaseName = dcsb.InitialCatalog;
            if( string.IsNullOrEmpty( targetDatabaseName ) )
            {
                return m.SendErrorAndDisplayHelp( "The connection string does not point to a database." ) == Program.RetCodeSuccess;
            }
            dcsb.InitialCatalog = "master";

            m.Trace( $"Target connection string: {TargetConnectionString = connectionString}" );
            m.Trace( $"Database name: {TargetDatabaseName = targetDatabaseName}" );
            m.Trace( $"Effective connection string: {MasterConnectionString = dcsb.ToString()}" );

            return true;
        }

        public SqlConnection CreateOpenedConnection( ConsoleMonitor m, bool useMasterConnectionString )
        {
            SqlConnection sqlConn = new SqlConnection( useMasterConnectionString ? MasterConnectionString : TargetConnectionString );
            sqlConn.InfoMessage += ( s, ev ) =>
            {
                foreach( SqlError info in ev.Errors )
                {
                    if( info.Class > 10 ) m.Error( info.Message );
                    else m.Info( info.Message );
                }
            };
            sqlConn.Open();
            return sqlConn;
        }
    }
}
