using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CK.Core;
using CK.Setup;
using Microsoft.Extensions.CommandLineUtils;
using CK.Text;

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

            var connectionStringArg = c.Argument(
                "ConnectionString",
                "SQL Server connection string used, pointing to the target database. The database will be created if it does not exist.",
                false
                );

            var assembliesOpt = c.Option(
                "-a|--assemblies",
                "Names of the assemblies to setup in the database, ignoring their dependencies.",
                CommandOptionType.MultipleValue
                );

            var recurseAssembliesOpt = c.Option(
                "-ra|--recurseAssemblies",
                "Names of the assemblies to setup in the database including their dependencies.",
                CommandOptionType.MultipleValue
                );

            var binPathOpt = c.Option(
                "-p|--binPath",
                "Path to the directory containing the assembly files, and in which the generated assembly will be saved. Defaults to the current working directory.",
                CommandOptionType.SingleValue
                );

            var generatedAssemblyNameOpt = c.Option(
                "-n|--generatedAssemblyName",
                $"Assembly name, and file name (without the .dll suffix) of the generated assembly. Defaults to {BuilderFinalAssemblyConfiguration.DefaultAssemblyName}.",
                CommandOptionType.SingleValue
                );

            //var sourceGenerationOpt = c.Option(
            //    "-sg|--sourceGeneration",
            //    $"Use the new code source generation (instead of IL emit).",
            //    CommandOptionType.NoValue
            //    );

            var sampleUsage = $@"Sample usage: {c.Parent.Name} {c.Name} ""Server=.;Database=Test;Integrated Security=true;"" -ra ""Super.Data"" -r ""Another.Model"" -p ""C:\App\Prod\SuperApp\bin""  -n ""Super.Generated"" ";


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
                List<string> recurseAssemblyNames;
                string binPath = Environment.CurrentDirectory;
                string generatedAssemblyName = BuilderFinalAssemblyConfiguration.DefaultAssemblyName;
                SetupEngineRunningMode runningMode = SetupEngineRunningMode.Default;
                bool sourceGeneration;

                connectionString = connectionStringArg.Value?.Trim();

                assemblyNames = assembliesOpt.Values
                    .Where( x => !string.IsNullOrWhiteSpace( x ) )
                    .Select( x => x.Trim() )
                    .ToList();

                recurseAssemblyNames = recurseAssembliesOpt.Values
                    .Where( x => !string.IsNullOrWhiteSpace( x ) )
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

                sourceGeneration = false; // sourceGenerationOpt.HasValue(); 

                // No connectionString given
                if( string.IsNullOrEmpty( connectionString ) )
                {
                    return DisplayErrorAndExit( c, sampleUsage, "A connection string is required." );
                }

                c.ShowRootCommandFullNameAndVersion();

                monitor.Trace().Send( $"Connection string: {connectionString}" );
                monitor.Trace().Send( $"Assembly names: {assemblyNames.Concatenate()}" );
                monitor.Trace().Send( $"Recurse Assembly names: {recurseAssemblyNames.Concatenate()}" );
                monitor.Trace().Send( $"Binaries path: {binPath}" );
                monitor.Trace().Send( $"Generated assembly name: {generatedAssemblyName}" );
                //monitor.Trace().Send( $"Source Generation: {sourceGeneration}" );

                var buildConfig = DbSetupHelper.BuildSetupConfig( connectionString, assemblyNames, recurseAssemblyNames, generatedAssemblyName, binPath, runningMode, sourceGeneration );

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
                        isSuccess = StObjContextRoot.Build( buildConfig, null, monitor );
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
                    m.Warn().Send( $"Failed to resolve assembly {assemblyName.Name} (File not found: {dllPath})" );
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
