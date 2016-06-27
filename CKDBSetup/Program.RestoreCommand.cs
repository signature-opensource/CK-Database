using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using CK.Core;
using CK.SqlServer;
using Microsoft.Extensions.CommandLineUtils;

namespace CKDBSetup
{
    static partial class Program
    {
        private static void RestoreCommand( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Restores a SQL Server database from a backup file, automatically moving its data and log files.";

            PrepareHelpOption( c );
            PrepareVersionOption( c );
            var logLevelOpt = PrepareLogLevelOption(c);
            var logFileOpt = PrepareLogFileOption(c);

            var connectionStringArg = c.Argument(
                "ConnectionString",
                "SQL Server connection string used, pointing to the target (restored) database.",
                false
                );

            var backupPathArg = c.Argument(
                "BackupFilePath",
                "Path to the backup file to restore, on the machine hosting the SQL Server instance. It must be readable by the SQL Server service. Non-absolute paths will be resolved relative to the default server backup directory.",
                false
                );

            c.OnExecute( () =>
            {
                var monitor = PrepareActivityMonitor(logLevelOpt, logFileOpt);

                // Invalid LogFilter
                if( monitor == null )
                {
                    Error.WriteLine( LogFilterErrorDesc );
                    c.ShowHelp();
                    return EXIT_ERROR;
                }

                string connectionString = connectionStringArg.Value;
                string backupPath = backupPathArg.Value;

                // No connectionString given
                if( string.IsNullOrEmpty( connectionString ) )
                {
                    Error.WriteLine( "\nError: A connection string is required." );
                    c.ShowHelp();
                    return EXIT_ERROR;
                }

                // No backup path given
                if( string.IsNullOrEmpty( backupPath ) )
                {
                    Error.WriteLine( "\nError: A path to the backup file is required." );
                    c.ShowHelp();
                    return EXIT_ERROR;
                }

                SqlConnectionStringBuilder dcsb = new SqlConnectionStringBuilder(connectionString);

                string targetDatabaseName = dcsb.InitialCatalog;

                // No backup path given
                if( string.IsNullOrEmpty( targetDatabaseName ) )
                {
                    Error.WriteLine( "\nError: The connection string does not specify a database (InitialCatalog)." );
                    c.ShowHelp();
                    return EXIT_ERROR;
                }

                dcsb.InitialCatalog = "master";

                monitor.Trace().Send( "Target connection string: {0}", connectionString );
                monitor.Trace().Send( "Path to backup: {0}", backupPath );
                monitor.Trace().Send( "Restored database name: {0}", targetDatabaseName );
                monitor.Trace().Send( "Effective connection string: {0}", dcsb.ToString() );

                try
                {
                    using( SqlConnection sqlConn = new SqlConnection( dcsb.ToString() ) )
                    {
                        sqlConn.InfoMessage += ( s, ev ) =>
                        {
                            foreach( SqlError info in ev.Errors )
                            {
                                if( info.Class > 10 )
                                {
                                    monitor.Error().Send( info.Message );
                                }
                                else
                                {
                                    monitor.Info().Send( info.Message );
                                }
                            }
                        };

                        sqlConn.Open();

                        if( !Path.IsPathRooted( backupPath ) )
                        {
                            monitor.Info().Send( "Path {0} is not absolute: Using default server backup directory.", backupPath );
                            string defaultBackupPath = GetDefaultServerBackupPath(monitor, sqlConn);
                            monitor.Trace().Send( "Default server backup path: {0}", defaultBackupPath );

                            backupPath = Path.GetFullPath( Path.Combine( defaultBackupPath, backupPath ) );
                        }

                        monitor.Info().Send( "Restoring from backup file: {0}", backupPath );

                        string dataDir = GetDefaultServerDataPath(monitor, sqlConn);
                        monitor.Trace().Send( "Server data directory: {0}", dataDir );

                        string logDir = GetDefaultServerLogPath(monitor, sqlConn);
                        monitor.Trace().Send( "Server log directory: {0}", logDir );

                        string q = BuildRestoreQuery(monitor, sqlConn, targetDatabaseName, backupPath, dataDir, logDir);

                        monitor.Trace().Send( "Calling: {0}", q );

                        using( SqlCommand cmd = new SqlCommand( q, sqlConn ) )
                        {
                            using( monitor.OpenTrace().Send( "SQL Server command execution", q ) )
                            {
                                int result = cmd.ExecuteNonQuery();
                            }
                        }

                        sqlConn.Close();
                    }
                }
                catch( Exception e )
                {
                    monitor.Fatal().Send( e, "Restore failed" );
                    return EXIT_ERROR;
                }

                monitor.Info().Send( "Restore from file successful." );

                return EXIT_SUCCESS;
            } );
        }

        private static string BuildRestoreQuery( IActivityMonitor m, SqlConnection sqlConn, string targetDatabaseName, string backupPath, string dataDir, string logDir )
        {
            if( !ValidateIdentifier( targetDatabaseName ) )
            {
                throw new ArgumentException( $"Invalid SQL Server identifier: {targetDatabaseName}" );
            }

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

            options.Add( $"MOVE '{SqlHelper.SqlEncodeString( logicalNames.Item1 )}' TO '{SqlHelper.SqlEncodeString( dataFileNewPath )}'" );
            options.Add( $"MOVE '{SqlHelper.SqlEncodeString( logicalNames.Item2 )}' TO '{SqlHelper.SqlEncodeString( logFileNewPath )}'" );

            backupPath = SqlHelper.SqlEncodeString( backupPath );

            StringBuilder sb = new StringBuilder();

            sb.Append( "RESTORE DATABASE " );
            sb.Append( targetDatabaseName );
            sb.Append( " FROM DISK = '" );
            sb.Append( backupPath );
            sb.Append( "'" );

            if( options.Count > 0 )
            {
                sb.Append( " WITH " );
                sb.Append( String.Join( ", ", options ) );
            }

            return sb.ToString();
        }

        private static Tuple<string, string> GetBackupDataAndLogLogicalNames( IActivityMonitor m, SqlConnection c, string backupPath )
        {
            backupPath = SqlHelper.SqlEncodeString( backupPath );

            StringBuilder sb = new StringBuilder();

            sb.Append( "RESTORE FILELISTONLY" );
            sb.Append( " FROM DISK = '" );
            sb.Append( backupPath );
            sb.Append( "'" );

            string dataLogicalName = null;
            string logLogicalName = null;

            m.Trace().Send( $"Executing: {0}", sb.ToString() );
            using( SqlCommand cmd = new SqlCommand( sb.ToString(), c ) )
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
