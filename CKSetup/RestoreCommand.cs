using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using CK.Core;
using Microsoft.Extensions.CommandLineUtils;
using CK.Text;

namespace CKSetup
{
    static class RestoreCommand
    {
        public static void Define( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Restores a SQL Server database from a backup file, automatically moving its data and log files.";
            c.StandardConfiguration( true );
            ConnectionStringArgument connectionArg = c.AddConnectionStringArgument();
            BackupPathArgument backupPathArg = c.AddBackupPathArgument( "Path to the backup file to restore, on the machine hosting the SQL Server instance. It must be readable by the SQL Server service. Non-absolute paths will be resolved relative to the default server backup directory." );

            c.OnExecute( monitor =>
            {
                if( !connectionArg.Initialize( monitor ) ) return Program.RetCodeError;
                using( var sqlConn = connectionArg.CreateOpenedConnection( monitor, useMasterConnectionString: true ) )
                {
                    if( !backupPathArg.Initialize( monitor, sqlConn ) ) return Program.RetCodeError;

                    string dataDir = SqlServerHelper.GetDefaultServerDataPath( monitor, sqlConn );
                    monitor.Trace().Send( "Server data directory: {0}", dataDir );

                    string logDir = SqlServerHelper.GetDefaultServerLogPath( monitor, sqlConn );
                    monitor.Trace().Send( "Server log directory: {0}", logDir );

                    string q = BuildRestoreQuery( monitor, sqlConn, connectionArg.TargetDatabaseName, backupPathArg.BackupPath, dataDir, logDir );

                    monitor.Trace().Send( "Calling: {0}", q );
                    using( SqlCommand cmd = new SqlCommand( q, sqlConn ) )
                    {
                        using( monitor.OpenTrace().Send( "SQL Server command execution", q ) )
                        {
                            int result = cmd.ExecuteNonQuery();
                        }
                    }
                }
                return Program.RetCodeSuccess;
            } );
        }

        private static string BuildRestoreQuery( IActivityMonitor m, SqlConnection sqlConn, string targetDatabaseName, string backupPath, string dataDir, string logDir )
        {
            var logicalNames = GetBackupDataAndLogLogicalNames( m, sqlConn, backupPath );

            List<string> options = new List<string>()
            {
                "REPLACE",
                "STATS",
            };

            string dataFileNewPath = Path.Combine( dataDir, $"{targetDatabaseName}.mdf" );
            string logFileNewPath = Path.Combine( logDir, $"{targetDatabaseName}_log.ldf" );

            m.Info().Send( "Restoring data file to: {0}", dataFileNewPath );
            m.Info().Send( "Restoring log file to: {0}", logFileNewPath );

            options.Add( $"MOVE '{SqlServerHelper.EncodeStringContent( logicalNames.Item1 )}' TO '{SqlServerHelper.EncodeStringContent( dataFileNewPath )}'" );
            options.Add( $"MOVE '{SqlServerHelper.EncodeStringContent( logicalNames.Item2 )}' TO '{SqlServerHelper.EncodeStringContent( logFileNewPath )}'" );

            StringBuilder sb = new StringBuilder();

            sb.Append( "RESTORE DATABASE " )
              .Append( targetDatabaseName )
              .Append( " FROM DISK = '" )
              .Append( SqlServerHelper.EncodeStringContent( backupPath ) )
              .Append( "'" );
            if( options.Count > 0 )
            {
                sb.Append( " WITH " );
                sb.AppendStrings( options );
            }
            return sb.ToString();
        }

        private static Tuple<string, string> GetBackupDataAndLogLogicalNames( IActivityMonitor m, SqlConnection c, string backupPath )
        {
            string restore = new StringBuilder()
                                    .Append( "RESTORE FILELISTONLY" )
                                    .Append( " FROM DISK = '" )
                                    .Append( SqlServerHelper.EncodeStringContent( backupPath ) )
                                    .Append( "'" )
                                    .ToString();

            string dataLogicalName = null;
            string logLogicalName = null;

            m.Trace().Send( $"Executing: {0}", restore );
            using( SqlCommand cmd = new SqlCommand( restore, c ) )
            {
                using( SqlDataReader r = cmd.ExecuteReader() )
                {
                    while( r.Read() )
                    {
                        string logicalName = r.GetString(0);
                        string physicalName = r.GetString(1);
                        string type = r.GetString(2);
                        long fileId = r.GetInt64(6);

                        m.Trace().Send( $"{fileId}: [{type}] {logicalName} ({physicalName})" );
                        if( type == "D" )
                        {
                            if( dataLogicalName != null ) { throw new NotSupportedException( "Complex backups with multiple data files are not supported" ); }
                            dataLogicalName = logicalName;
                        }
                        else if( type == "L" )
                        {
                            if( logLogicalName != null ) { throw new NotSupportedException( "Complex backups with multiple lognote files are not supported" ); }
                            logLogicalName = logicalName;
                        }
                    }
                }
            }

            if( dataLogicalName == null ) { throw new NotSupportedException( "Backups without data files are not supported" ); }
            if( logLogicalName == null ) { throw new NotSupportedException( "Backups without log files are not supported" ); }

            return Tuple.Create( dataLogicalName, logLogicalName );
        }
    }
}
