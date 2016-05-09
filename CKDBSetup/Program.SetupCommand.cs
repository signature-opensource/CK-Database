using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CK.Core;
using CK.Setup;
using Microsoft.Extensions.CommandLineUtils;
#if !CKDBLEGACY
using CK.Text;
#endif

namespace CKDBSetup
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

            // Sample usage: CKDBSetup setup "Server=.;Database=MyDatabase;Integrated Security=true;" My.Assembly1 My.Assembly2

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
                $"Assembly name, and file name (without the .dll suffix) of the generated structure assembly. Defaults to {BuilderFinalAssemblyConfiguration.DefaultAssemblyName}.",
                CommandOptionType.SingleValue
                );

            var generateAssemblyOnlyOpt = c.Option(
                "--generateAssemblyOnly",
                @"Generates the structure assembly without setting up the database.",
                CommandOptionType.NoValue
                );

            var sampleUsage = $"\nSample usage: {c.Parent.Name} {c.Name} \"Server=.;Database=MyDatabase;Integrated Security=true;\" CK.DB.Actor My.Assembly1\n";


            c.OnExecute( () =>
            {
                var monitor = PrepareActivityMonitor(logLevelOpt, logFileOpt);

                // Invalid LogFilter
                if( monitor == null )
                {
                    return DisplayErrorAndExit( c, sampleUsage, LogFilterErrorDesc );
                }

                string connectionString;
                List<string> assemblyNames;
                string binPath = Environment.CurrentDirectory;
                string generatedAssemblyName = BuilderFinalAssemblyConfiguration.DefaultAssemblyName;
                SetupEngineRunningMode runningMode = SetupEngineRunningMode.Default;

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
                    return DisplayErrorAndExit( c, sampleUsage, "A connection string is required." );
                }
                // No assembly name given
                if( assemblyNames.Count < 1 )
                {
                    return DisplayErrorAndExit( c, sampleUsage, "One or more assembly names are required." );
                }

                // Handle running mode
                if( generateAssemblyOnlyOpt.HasValue() )
                {
                    runningMode = SetupEngineRunningMode.StObjLayerOnly;
                }

                c.ShowRootCommandFullNameAndVersion();

                monitor.Trace().Send( $"Connection string: {connectionString}" );
                monitor.Trace().Send( $"Assembly names: {assemblyNames.Concatenate()}" );
                monitor.Trace().Send( $"Binaries path: {binPath}" );
                monitor.Trace().Send( $"Generated assembly name: {generatedAssemblyName}" );
                monitor.Trace().Send( $"Running mode: {runningMode}" );

                var buildConfig = DbSetupHelper.BuildSetupConfig( connectionString, assemblyNames, generatedAssemblyName, binPath, runningMode );

                bool isSuccess = false;

                List<ActivityMonitorSimpleCollector.Entry> errorEntries = new List<ActivityMonitorSimpleCollector.Entry>();

                // We need to manually hook Assembly resolution to allow DbSetup to probe the correct one.
                ResolveEventHandler reh = (s, a) =>
                {
                    monitor.Trace().Send( $"AssemblyResolve: {a.Name}" );
                    return LoadAssembly( monitor, binPath, a.Name );
                };
                AppDomain.CurrentDomain.AssemblyResolve += reh;

                // Execution
                using( monitor.CollectEntries( errorEntries.AddRange, LogLevelFilter.Error ) )
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

        private static Assembly LoadAssembly( IActivityMonitor m, string binPath, string name )
        {
            using( m.OpenTrace().Send( "Loading manually: {0}", name ) )
            {
                AssemblyName assemblyName = new AssemblyName(name);
                string dllPath = Path.Combine( binPath, assemblyName.Name + ".dll" );

                m.Trace().Send( $"Manually resolving assembly {assemblyName.Name} in: {dllPath}" );

                if( File.Exists( dllPath ) )
                {
                    // Don't use LoadFrom(), as it will fork into its own assembly load context.
                    return Assembly.LoadFile( dllPath );
                }
                else
                {
                    m.Error().Send( $"Failed to resolve assembly {assemblyName.Name} (File not found: {dllPath})" );
                    return null;
                }
            }
        }

        static int DisplayErrorAndExit( CommandLineApplication c, string sampleUsage, string msg )
        {
            Error.WriteLine( "\nError: " + msg );
            c.ShowHelp();
            Error.WriteLine( sampleUsage );
            return EXIT_ERROR;
        }
    }
}
