using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using Microsoft.Extensions.CommandLineUtils;

namespace CkDbSetup
{
    static partial class Program
    {
        private static void BackupCommand( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Calls a full independent backup of a SQL Server database to file on the SQL Server instance.";

            PrepareHelpOption( c );
            PrepareVersionOption( c );
            var logLevelOpt = PrepareLogLevelOption(c);
            var logFileOpt = PrepareLogFileOption(c);

            var connectionStringArg = c.Argument(
                "ConnectionString",
                "SQL Server connection string used, pointing to the target database.",
                false
                );

            var backupPathArg = c.Argument(
                "BackupFilePath",
                "Path to the new backup file on the machine hosting the SQL Server instance. It must be absolute, and writable by the SQL Server service.",
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
                    Error.WriteLine( "\nError: The connection string does not point to a database." );
                    c.ShowHelp();
                    return EXIT_ERROR;
                }

                dcsb.InitialCatalog = "master";

                monitor.Trace().Send( "Target connection string: {0}", connectionString );
                monitor.Trace().Send( "Path to backup: {0}", backupPath );
                monitor.Trace().Send( "Database name: {0}", targetDatabaseName );
                monitor.Trace().Send( "Effective connection string: {0}", dcsb.ToString() );

                try
                {
                    using( SqlConnection sqlConn = new SqlConnection( connectionString ) )
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


                        monitor.Info().Send( "Effective backup path: {0}", backupPath );

                        string q = BuildBackupQuery(targetDatabaseName, backupPath);

                        monitor.Trace().Send( "Calling: {0}", q );

                        using( SqlCommand cmd = new SqlCommand( q, sqlConn ) )
                        {
                            int result = cmd.ExecuteNonQuery();
                            monitor.Trace().Send( "Non-query returned {0}", result );
                        }

                        sqlConn.Close();
                    }
                }
                catch( Exception e )
                {
                    monitor.Fatal().Send( e, "Backup failed" );
                    return EXIT_ERROR;
                }

                monitor.Info().Send( "Backup to file successful." );

                return EXIT_SUCCESS;
            } );
        }

        private static string BuildBackupQuery( string targetDatabaseName, string backupPath )
        {
            if( !ValidateIdentifier( targetDatabaseName ) )
            {
                throw new ArgumentException( $"Invalid SQL Server identifier: {targetDatabaseName}" );
            }

            List<string> options = new List<string>()
            {
                "COPY_ONLY",
                "FORMAT",
                "STATS",
                "NAME='CkDbSetup backup'",
                "DESCRIPTION='CkDbSetup backup'"
            };

            backupPath = SqlHelper.SqlEncode( backupPath );

            StringBuilder sb = new StringBuilder();

            sb.Append( "BACKUP DATABASE " );
            sb.Append( targetDatabaseName );
            sb.Append( " TO DISK = '" );
            sb.Append( backupPath );
            sb.Append( "'" );

            if( options.Count > 0 )
            {
                sb.Append( " WITH " );
                sb.Append( String.Join( ", ", options ) );
            }

            return sb.ToString();
        }

        private static string GetDefaultServerBackupPath( IActivityMonitor m, SqlConnection c )
        {
            using( SqlCommand cmd = new SqlCommand( "master.dbo.xp_instance_regread", c ) )
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var p1 = cmd.Parameters.Add( "@rootkey", SqlDbType.NVarChar );
                p1.Size = 256;
                p1.Direction = ParameterDirection.Input;
                p1.Value = @"HKEY_LOCAL_MACHINE";

                var p2 = cmd.Parameters.Add( "@key", SqlDbType.NVarChar );
                p2.Size = 256;
                p2.Direction = ParameterDirection.Input;
                p2.Value = @"Software\Microsoft\MSSQLServer\MSSQLServer";

                var p3 = cmd.Parameters.Add( "@value_name", SqlDbType.NVarChar );
                p3.Size = 256;
                p3.Direction = ParameterDirection.Input;
                p3.Value = @"BackupDirectory";

                var valParam = cmd.Parameters.Add( "@value", SqlDbType.NVarChar );
                valParam.Size = 256;
                valParam.Direction = ParameterDirection.Output;

                m.Trace().Send( "Executing command: {0}", cmd.CommandText );
                int r = cmd.ExecuteNonQuery();
                m.Trace().Send( "Non-query returned {0}", r );

                string path = (string)valParam.Value;
                return path;
            }
        }

        private static readonly Regex SqlServerIdentifierRegex = new Regex(@"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$");

        private static bool ValidateIdentifier( string name ) => SqlServerIdentifierRegex.IsMatch( name );
    }
}
