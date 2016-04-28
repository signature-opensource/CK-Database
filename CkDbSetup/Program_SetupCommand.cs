using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;
using Microsoft.Extensions.CommandLineUtils;

namespace CkDbSetup
{
    static partial class Program
    {
        private static void SetupCommand( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Sets up the given CK.Database assemblies in a SQL Server instance, using the given SQL Server connection string to connect, and generates a structure (StObjMap) assembly.";

            PrepareHelpOption( c );
            PrepareVersionOption( c );
            var logLevelOpt = PrepareLogLevelOption(c);
            var logFileOpt = PrepareLogFileOption(c);

            // Sample usage: CkDbSetup setup "Server=.;Database=MyDatabase;Integrated Security=true;" My.Assembly1 My.Assembly2

            var connectionStringArg = c.Argument(
                "ConnectionString",
                "SQL Server connection string used, pointing to the target database. The database will be created if it does not exist.",
                false
                );

            var assembliesArg = c.Argument(
                "AssemblyNames",
                "Names of the assemblies to setup in the database.",
                true
                );

            var binPathOpt = c.Option(
                "-p|--binPath",
                "Path to the directory containing the assembly files, and in which the generated structure assembly will be saved. Defaults to the current working directory.",
                CommandOptionType.SingleValue
                );

            var generatedAssemblyNameOpt = c.Option(
                "-n|--generatedAssemblyName",
                $"Assembly name, and file name (without the .dll suffix) of the generated structure assembly. Defaults to {DefaultGeneratedAssemblyName}.",
                CommandOptionType.SingleValue
                );

            var sampleUsage = $"\nSample usage: {c.Parent.Name} {c.Name} \"Server=.;Database=MyDatabase;Integrated Security=true;\" My.Assembly1 My.Assembly2\n";


            c.OnExecute( () =>
            {
                var monitor = PrepareActivityMonitor(logLevelOpt, logFileOpt);

                // Invalid LogFilter
                if( monitor == null )
                {
                    Error.WriteLine( LogFilterErrorDesc );
                    c.ShowHelp();
                    Error.WriteLine( sampleUsage );
                    return EXIT_ERROR;
                }

                string connectionString;
                List<string> assemblyNames;
                string binPath = Environment.CurrentDirectory;
                string generatedAssemblyName = DefaultGeneratedAssemblyName;

                connectionString = connectionStringArg.Value?.Trim();

                assemblyNames = assembliesArg.Values
                    .Where( x => !String.IsNullOrWhiteSpace( x ) )
                    .Select( x => x.Trim() )
                    .ToList();

                if( binPathOpt.HasValue() )
                {
                    binPath = Path.GetFullPath( binPathOpt.Value() );
                }

                if( generatedAssemblyNameOpt.HasValue() )
                {
                    generatedAssemblyName = generatedAssemblyNameOpt.Value().Trim();
                }
                // No connectionString given
                if( string.IsNullOrEmpty( connectionString ) )
                {
                    Error.WriteLine( "\nError: A connection string is required." );
                    c.ShowHelp();
                    Error.WriteLine( sampleUsage );
                    return EXIT_ERROR;
                }

                // No assembly name given
                if( assemblyNames.Count < 1 )
                {
                    Error.WriteLine( "\nError: One or more assembly names are required." );
                    c.ShowHelp();
                    Error.WriteLine( sampleUsage );
                    return EXIT_ERROR;
                }

                c.ShowRootCommandFullNameAndVersion();

                monitor.Trace().Send( $"Connection string: {connectionString}" );
                monitor.Trace().Send( $"Assembly names: {string.Join( "; ", assemblyNames )}" );
                monitor.Trace().Send( $"Binaries path: {binPath}" );
                monitor.Trace().Send( $"Generated assembly name: {generatedAssemblyName}" );

                var buildConfig = DbSetupHelper.BuildSetupConfig( connectionString, assemblyNames, generatedAssemblyName, binPath );

                bool isSuccess = false;

                List<ActivityMonitorSimpleCollector.Entry> errorEntries = new List<ActivityMonitorSimpleCollector.Entry>();

                // We need to manually hook Assembly resolution to allow DbSetup to probe the correct one.
                ResolveEventHandler reh = (s, a) =>
                {
                    AssemblyName an = new AssemblyName(a.Name);
                    string dllPath = Path.Combine( binPath, an.Name + ".dll" );

                    monitor.Trace().Send($"Manually resolving assembly {a.Name} in: {dllPath}");

                    if(File.Exists(dllPath))
                    {
                        return Assembly.LoadFrom( dllPath );
                    }
                    else
                    {
                        monitor.Error().Send($"Failed to resolve assembly {a.Name} (File not found: {dllPath})");
                        return null;
                    }
                };
                AppDomain.CurrentDomain.AssemblyResolve += reh;
                // Execution
                using( monitor.CollectEntries( errors =>
                {
                    errorEntries.AddRange( errors );
                }, LogLevelFilter.Error ) )
                {
                    try
                    {
                        isSuccess = DbSetupHelper.ExecuteDbSetup( monitor, buildConfig );
                    }
                    catch( Exception e )
                    {
                        monitor.Fatal().Send( e );
                    }
                }
                AppDomain.CurrentDomain.AssemblyResolve -= reh;

                // Summary log entry
                if( !isSuccess )
                {
                    using( monitor.OpenFatal().Send( $"Database setup failed with {errorEntries.Count} error{(errorEntries.Count > 1 ? "s" : "")}" ) )
                    {
                        foreach( var e in errorEntries )
                        {
                            monitor.Error().Send( e.Exception, e.Tags, e.Text );
                        }
                    }
                }
                else if( errorEntries.Count > 0 )
                {
                    using( monitor.OpenWarn().Send( $"Database setup succeeded, but encountered {errorEntries.Count} error{(errorEntries.Count > 1 ? "s" : "")}." ) )
                    {
                        foreach( var e in errorEntries )
                        {
                            monitor.Warn().Send( e.Exception, e.Tags, e.Text );
                        }
                    }
                }
                else
                {
                    monitor.Info().Send( "Database setup was successful." );
                }

                return isSuccess ? EXIT_SUCCESS : EXIT_ERROR;
            } );
        }
    }
}
