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
using CSemVer;

namespace CKSetup
{
    static class CommandLineApplicationExtension
    {
        public const string LogLevelOptionName = "logFilter";
        public const string LogFileOptionName = "logFile";
        public const string LogFilterDesc = "Valid log filters: \"Off\", \"Release\", \"Monitor\", \"Terse\", \"Verbose\", \"Debug\", or any \"{Group,Line}\" format where Group and Line can be: \"Debug\", \"Trace\", \"Info\", \"Warn\", \"Error\", \"Fatal\", or \"Off\".";

        static readonly InformationalVersion _thisVersion;
        static readonly string _longVersion;
        static readonly string _shortVersion;

        static CommandLineApplicationExtension()
        {
            var a = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute( Assembly.GetExecutingAssembly(), typeof( AssemblyInformationalVersionAttribute ) );
            _thisVersion = new InformationalVersion( a?.InformationalVersion );
            _longVersion = _thisVersion.ToString();
            _shortVersion = _thisVersion.NuGetVersion?.Text ?? "<No valid version>";
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

        static public StoreBinFolderArguments AddStoreDirArguments( this CommandLineApplication @this, string description )
        {
            return new StoreBinFolderArguments( @this.Argument( "BinFolders",
                                               description,
                                               true ) );
        }

        static public BinPathsOption AddBinPathsOption( this CommandLineApplication @this, string description )
        {
            return new BinPathsOption( @this.Option(
                                      "-p|--binPath",
                                      description,
                                      CommandOptionType.MultipleValue ) );
        }

        static public StorePathOptions AddStorePathOption( this CommandLineApplication @this )
        {
            return new StorePathOptions( @this.Option(
                                         "--store",
                                         $"Full path of the runtime store to use (can be a .zip or a directory). Defaults to: {StorePathOptions.DefaultStorePath}.",
                                         CommandOptionType.SingleValue ) );
        }

        static public StorePushOptions AddStorePushOptions( this CommandLineApplication @this )
        {
            return new StorePushOptions( @this.Option( 
                                            "-u|--url",
                                            $"Url of the remote store. Defaults to: {StorePushOptions.DefaultStoreUrl}.",
                                            CommandOptionType.SingleValue ),
                                         @this.Option(
                                            "-k|--apiKey",
                                            $"Api key for the remote.",
                                            CommandOptionType.SingleValue ) );
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
                        if( r == Program.RetCodeSuccess ) monitor.Info( $"Command {@this.Name} succeed." );
                        return r;
                    }
                    catch( Exception ex )
                    {
                        monitor.Fatal( $"Command {@this.Name} failed.", ex );
                        return Program.RetCodeError;
                    }
                }
            } );
        }

    }
}
