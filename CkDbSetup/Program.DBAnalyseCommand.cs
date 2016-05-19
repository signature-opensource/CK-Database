using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using CK.Core;
using CK.SqlServer;
using Microsoft.Extensions.CommandLineUtils;
using CK.SqlServer.Parser;

namespace CKDBSetup
{
    static partial class Program
    {
        private static void DBAnalyzeCommand( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Analyses a SQL Server database from a connection string.";

            PrepareHelpOption( c );
            PrepareVersionOption( c );
            var logLevelOpt = PrepareLogLevelOption(c);
            var logFileOpt = PrepareLogFileOption(c);

            var connectionStringArg = c.Argument(
                "ConnectionString",
                "SQL Server connection string used, pointing to the target database.",
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

                // No connectionString given
                if( string.IsNullOrEmpty( connectionString ) )
                {
                    Error.WriteLine( "\nError: A connection string is required." );
                    c.ShowHelp();
                    return EXIT_ERROR;
                }

                try
                {
                    using( SqlConnection con = new SqlConnection( connectionString ) )
                    {
                        con.InfoMessage += ( s, ev ) =>
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
                        con.Open();

                        using( var cmd = new SqlCommand( $@"
                            select s.name, p.name, OBJECT_DEFINITION(OBJECT_ID(s.name + '.' + p.name)) 
	                            from sys.procedures p
	                            inner join sys.schemas s on s.schema_id = p.schema_id", con ) )
                        {
                            ISqlServerParser parser = new SqlServerParser();
                            using( var r = cmd.ExecuteReader() )
                            {
                                while( r.Read() )
                                {
                                    try
                                    {
                                        string schema = r.GetString( 0 );
                                        string name = r.GetString( 1 );
                                        string fullBody = r.GetString( 2 );
                                        var result = parser.ParseStoredProcedure( fullBody );
                                        if( result.IsError )
                                        {
                                            result.LogOnError( monitor );
                                            using( monitor.OpenTrace().Send( "Full text:" ) )
                                            {
                                                monitor.Trace().Send( fullBody );
                                            }
                                        }
                                        else monitor.Trace().Send( "Successfuly parsed: " + result.Result.ToStringSignature( true ) );
                                    }
                                    catch( Exception ex )
                                    {
                                        monitor.Error().Send( ex );
                                    }
                                }
                            }
                        }
                    }
                }
                catch( Exception e )
                {
                    monitor.Fatal().Send( e, "Unable to connect to database." );
                    return EXIT_ERROR;
                }

                monitor.Info().Send( "Analysis end." );

                return EXIT_SUCCESS;
            } );
        }

    }
}
