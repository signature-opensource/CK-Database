using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CK.Core;
using System.Runtime.CompilerServices;
using System.IO;

namespace CKSetup
{
    static class CommandLineApplicationExtension
    {
        public const string LogLevelOptionName = "logFilter";
        public const string LogFileOptionName = "logFile";
        public const string LogFilterDesc = "Valid log filters: \"Off\", \"Release\", \"Monitor\", \"Terse\", \"Verbose\", \"Debug\", or any \"{Group,Line}\" format where Group and Line can be: \"Debug\", \"Trace\", \"Info\", \"Warn\", \"Error\", \"Fatal\", or \"Off\".";

        static readonly string _longVersion;
        static readonly string _shortVersion;

        static CommandLineApplicationExtension()
        {
            var a = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute( Assembly.GetExecutingAssembly(), typeof( AssemblyInformationalVersionAttribute ) );
            _longVersion = a != null ? a.InformationalVersion : "Not a valid version";
            _shortVersion = a != null
                               ? Regex.Match( a.InformationalVersion, @".*?\(?<1>.*?\)" ).Groups[1].Value
                               : "Not a valid version";
        }

        static public void StandardConfiguration( this CommandLineApplication @this, bool withMonitor )
        {
            @this.LongVersionGetter = () => _longVersion;
            @this.ShortVersionGetter = () => _shortVersion;
            @this.HelpOption( "-?|-h|--help" );
            @this.VersionOption( "-v|--version", @this.LongVersionGetter() );
            if( withMonitor )
            {
                @this.Option( $"-f|--{LogLevelOptionName}", 
                              $"Sets a log level filter for console and/or file output. {LogFilterDesc}", 
                              CommandOptionType.SingleValue );

                @this.Option( $"--l|--{LogFileOptionName}",
                              $"Path of a log file which will contain the log output. Defaults to none (console logging only).",
                              CommandOptionType.SingleValue );
            }
        }

        static public ConnectionStringArgument AddConnectionStringArgument( this CommandLineApplication @this )
        {
            return new ConnectionStringArgument( @this.Argument( "ConnectionString",
                                                 "SQL Server connection string used, pointing to the target database.",
                                                 false ) );
        }

        static public BackupPathArgument AddBackupPathArgument( this CommandLineApplication @this, string description )
        {
            return new BackupPathArgument( @this.Argument( "BackupFilePath",
                                           description,
                                            false ) );
        }

        static public ConsoleMonitor CreateConsoleMonitor( this CommandLineApplication @this )
        {
            return new ConsoleMonitor( @this );
        }

        static public void OnExecute( this CommandLineApplication @this, Func<ConsoleMonitor,int> invoke )
        {
            @this.OnExecute( () =>
            {
                using( var monitor = @this.CreateConsoleMonitor() )
                {
                    try
                    {
                        int r = invoke( monitor );
                        if( r == Program.RetCodeSuccess ) monitor.Info().Send( $"Command {@this.Name} succeed." );
                        return r;
                    }
                    catch( Exception e )
                    {
                        monitor.Fatal().Send( e, $"Command {@this.Name} failed." );
                        return Program.RetCodeError;
                    }
                }
            } );
        }

    }
}
