using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using CK.Core;
using Microsoft.Extensions.CommandLineUtils;
using CK.Text;
using Mono.Cecil;
using System.Linq;
using Mono.Collections.Generic;
using CK.Setup;
using System.Xml.Linq;
using System.Diagnostics;

namespace CKSetup
{
    /// <summary>
    /// Setup command implementation.
    /// </summary>
    static public class SetupCommand
    {
        public static void Define( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Sets up the given CK.Database assemblies in a SQL Server instance, using the given SQL Server connection string to connect, and generates a structure (StObjMap) assembly.";
            c.StandardConfiguration( true );
            ConnectionStringArgument connectionArg = c.AddConnectionStringArgument();
            BinPathsOption binPaths = c.AddBinPathsOption( "Path to the directories containing the assembly files, and in which the generated assembly will be saved. Defaults to the current working directory." );
            StorePathOption storePath = c.AddStorePathOption();

            var generatedAssemblyNameOpt = c.Option( "-n|--generatedAssemblyName",
                                                     $"Assembly name, and file name (without the .dll suffix) of the generated assembly. Defaults to 'CK.StObj.AutoAssembly'.",
                                                     CommandOptionType.SingleValue );
            var sourceGenerationOpt = c.Option(
                "-sg|--sourceGeneration",
                $"Use the new code source generation (instead of IL emit).",
                CommandOptionType.NoValue
                );

            c.OnExecute( monitor =>
            {
                if( !connectionArg.Initialize( monitor ) ) return Program.RetCodeError;
                if( !binPaths.Initialize( monitor ) ) return Program.RetCodeError;
                if( !storePath.Initialize( monitor, binPaths.BinPaths[0] ) ) return Program.RetCodeError;

                if( binPaths.BinPaths.Count > 1 )
                {
                    throw new NotImplementedException( "Multi Bin path Setup is not yet implemented." );
                }
                using( RuntimeArchive store = RuntimeArchive.OpenOrCreate( monitor, storePath.StorePath ) )
                {
                    if( store == null ) return Program.RetCodeError;
                    return DoSetup(
                            monitor,
                            binPaths.BinPaths[0],
                            store,
                            connectionArg.TargetConnectionString,
                            generatedAssemblyNameOpt.Value(),
                            sourceGenerationOpt.HasValue() );
                }
            } );
        }

        /// <summary>
        /// This is static in order to ease tests.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="binPath">The bin path to setup.</param>
        /// <param name="zip">The opened, valid, Zip runtime.</param>
        /// <param name="targetConnectionString">The default connection string.</param>
        /// <param name="generatedAssemblyName">Name of the assembly to generate.</param>
        /// <param name="sourceGeneration">True to generate source code.</param>
        /// <param name="missingImporter">Optional component importer.</param>
        /// <returns>Program return code (0 for success).</returns>
        public static int DoSetup( IActivityMonitor monitor, string binPath, RuntimeArchive zip, string targetConnectionString, string generatedAssemblyName, bool sourceGeneration, IComponentImporter missingImporter = null )
        {
            var binFolder = BinFolder.ReadBinFolder( monitor, binPath );
            if( binFolder == null ) return Program.RetCodeError;
            if( !zip.ExtractRuntimeDependencies( new[] { binFolder }, null, missingImporter ) ) return Program.RetCodeError;
            var toSetup = binFolder.Files.Where( b => !b.IsExcludedFromSetup 
                                                        && b.LocalDependencies.Any( dep => dep.ComponentKind == ComponentKind.Model ) )
                                            .Select( b => b.Name.Name );
            using( monitor.OpenTrace( "Creating setup configuration xml file." ) )
            {
                var config = BuildSetupConfig(
                                targetConnectionString,
                                toSetup,
                                generatedAssemblyName,
                                sourceGeneration );
                var configPath = WritDBSetupConfig( monitor, config, binPath );
                zip.RegisterFileToDelete( configPath );
            }
            return RunSetup( monitor, binPath );
        }

        static int RunSetup( IActivityMonitor m, string binPath )
        {
            using( m.OpenInfo( "Launching setup." ) )
            {
                bool useDotNet = false;
                string exe = Path.Combine( binPath, "CK.Setupable.Engine.exe" );
                string dll = Path.Combine( binPath, "CK.Setupable.Engine.dll" );
                if( !File.Exists( exe ) )
                {
                    if( !File.Exists( dll ) )
                    {
                        m.Error( "Unable to find CK.Setupable.Engine.exe runner in folder." );
                        return Program.RetCodeError;
                    }
                    useDotNet = true;
                }
                ProcessStartInfo cmdStartInfo = new ProcessStartInfo();
                cmdStartInfo.WorkingDirectory = binPath;
                cmdStartInfo.RedirectStandardOutput = true;
                cmdStartInfo.RedirectStandardError = true;
                cmdStartInfo.RedirectStandardInput = true;
                cmdStartInfo.UseShellExecute = false;
                cmdStartInfo.CreateNoWindow = true;
                if( useDotNet )
                {
                    cmdStartInfo.FileName = "dotnet";
                    cmdStartInfo.Arguments = "CK.Setupable.Engine.dll";
                }
                else
                {
                    cmdStartInfo.FileName = exe;
                }

                using( Process cmdProcess = new Process() )
                {
                    cmdProcess.StartInfo = cmdStartInfo;
                    cmdProcess.ErrorDataReceived += ( o, e ) => { if( !string.IsNullOrEmpty( e.Data ) ) m.Error( e.Data ); };
                    cmdProcess.OutputDataReceived += ( o, e ) =>
                    {
                        if( e.Data != null )
                        {
                            m.Info( e.Data );
                        }
                    };
                    cmdProcess.Start();
                    cmdProcess.BeginErrorReadLine();
                    cmdProcess.BeginOutputReadLine();
                    cmdProcess.WaitForExit();
                    return cmdProcess.ExitCode;
                }
            }
        }

        static string WritDBSetupConfig( IActivityMonitor m, SetupEngineConfiguration conf, string binPath )
        {
            // We have only one aspect to handle: the SqlSetupAspectConfiguration.
            Func<Type, string> typeWriter = t => "CK.Setup.SqlSetupAspectConfiguration, CK.SqlServer.Setup.Model";
            var doc = new XDocument(
                            new XElement( SetupRunner.xRunner,
                                new XElement( SetupRunner.xLogFiler, m.MinimalFilter.ToString() ),
                                conf.SerializeXml( new XElement( SetupRunner.xSetup ), typeWriter ) ) );
            string filePath = Path.Combine( binPath, SetupRunner.XmlFileName );
            string text = doc.ToString();
            m.Debug( text );
            File.WriteAllText( filePath, text );
            return filePath;
        }

        static SetupEngineConfiguration BuildSetupConfig(
            string connectionString,
            IEnumerable<string> assembliesToSetup,
            string dynamicAssemblyName,
            bool sourceGeneration )
        {
            var config = new SetupEngineConfiguration();
            config.RunningMode = SetupEngineRunningMode.Default;
            config.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.AddRange( assembliesToSetup );
            config.StObjEngineConfiguration.FinalAssemblyConfiguration.AssemblyName = dynamicAssemblyName;
            config.StObjEngineConfiguration.FinalAssemblyConfiguration.SourceGeneration = sourceGeneration;
            var c = new SqlSetupAspectConfiguration
            {
                DefaultDatabaseConnectionString = connectionString,
                IgnoreMissingDependencyIsError = true
            };
            config.Aspects.Add( c );
            return config;
        }
    }
}
