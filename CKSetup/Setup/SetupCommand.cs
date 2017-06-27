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
    static partial class SetupCommand
    {
        public static void Define( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Sets up the given CK.Database assemblies in a SQL Server instance, using the given SQL Server connection string to connect, and generates a structure (StObjMap) assembly.";
            c.StandardConfiguration( true );
            ConnectionStringArgument connectionArg = c.AddConnectionStringArgument();
            BinPathsOption binPaths = c.AddBinPathsOption( "Path to the directory containing the assembly files, and in which the generated assembly will be saved. Defaults to the current working directory." );
            ZipRuntimeFileOption zipFile = c.AddZipRuntimeFileOption();

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
                if( !zipFile.Initialize( monitor, binPaths.BinPaths[0] ) ) return Program.RetCodeError;

                if( binPaths.BinPaths.Count > 1 )
                {
                    throw new NotImplementedException( "Multi Bin path Setup is not yet implemented." );
                }
                string binPath = binPaths.BinPaths[0];
                var binFolder = BinFolder.ReadBinFolder( monitor, binPath );
                if( binFolder == null ) return Program.RetCodeError;
                using( ZipRuntimeArchive zip = ZipRuntimeArchive.OpenOrCreate( monitor, zipFile.ZipRuntimeFile ) )
                {
                    if( zip == null ) return Program.RetCodeError;
                    if( !zip.ExtractRuntimeDependencies( binFolder ) ) return Program.RetCodeError;
                    var toSetup = binFolder.Files.Where( b => b.LocalDependencies.Any( dep => dep.ComponentKind == ComponentKind.Model ) )
                                                    .Select( b => b.Name.Name );
                    using( monitor.OpenTrace().Send( "Creating setup configuration xml file." ) )
                    {
                        var config = BuildSetupConfig(
                                        connectionArg.TargetConnectionString,
                                        toSetup,
                                        generatedAssemblyNameOpt.Value(),
                                        sourceGenerationOpt.HasValue() );
                        var configPath = WritDBSetupConfig( monitor, config, binPath );
                        zip.RegisterFileToDelete( configPath );
                    }
                    using( monitor.OpenInfo().Send( "Launching setup." ) )
                    {
                        string exePath = Path.Combine( binPath, "CK.Setupable.Engine.exe" );
                        return RunSetup( monitor, exePath );
                    }
                }
            } );
        }

        static int RunSetup( IActivityMonitor m, string exePath )
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo();
            cmdStartInfo.FileName = exePath;
            cmdStartInfo.RedirectStandardOutput = true;
            cmdStartInfo.RedirectStandardError = true;
            cmdStartInfo.RedirectStandardInput = true;
            cmdStartInfo.UseShellExecute = false;
            cmdStartInfo.CreateNoWindow = true;

            using( Process cmdProcess = new Process() )
            {
                cmdProcess.StartInfo = cmdStartInfo;
                cmdProcess.ErrorDataReceived += ( o, e ) => { if( !string.IsNullOrEmpty( e.Data ) ) m.Error().Send( e.Data ); };
                cmdProcess.OutputDataReceived += ( o, e ) =>
                {
                    if( e.Data != null )
                    {
                        m.Info().Send( e.Data );
                    }
                };
                cmdProcess.Start();
                cmdProcess.BeginErrorReadLine();
                cmdProcess.BeginOutputReadLine();
                cmdProcess.WaitForExit();
                return cmdProcess.ExitCode;
            }
        }


        static string WritDBSetupConfig( IActivityMonitor m, SetupEngineConfiguration conf, string binPath )
        {
            var doc = new XDocument(
                            new XElement( SetupRunner.xRunner,
                                new XElement( SetupRunner.xLogFiler, SetupRunner.xLogFiler, m.MinimalFilter.ToString() ) ),
                            conf.SerializeXml( new XElement( SetupRunner.xSetup ) ) );
            string filePath = Path.Combine( binPath, SetupRunner.XmlFileName );
            doc.Save( filePath );
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
