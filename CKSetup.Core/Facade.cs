using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup
{
    static public class Facade
    {
        /// <summary>
        /// This is static in order to ease tests.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="binPath">The bin path to setup.</param>
        /// <param name="archive">The opened, valid, Zip runtime.</param>
        /// <param name="targetConnectionString">The default connection string.</param>
        /// <param name="generatedAssemblyName">Name of the assembly to generate.</param>
        /// <param name="sourceGeneration">True to generate source code.</param>
        /// <param name="missingImporter">Optional component importer.</param>
        /// <param name="remoteStoreUrl">Optional remote store url (ignored if a <paramref name="missingImporter"/> is specified).</param>
        /// <param name="debugBreakInCKStObjRunner">Calls Debugger.Launch() at the start of CK.StObj.Runner entry point.</param>
        /// <returns>True on success, false on error.</returns>
        public static bool DoSetup(
            IActivityMonitor monitor,
            string binPath,
            RuntimeArchive archive,
            string targetConnectionString,
            string generatedAssemblyName,
            bool sourceGeneration,
            IComponentImporter missingImporter = null,
            Uri remoteStoreUrl = null,
            bool debugBreakInCKStObjRunner = false )
        {
            using( monitor.OpenTrace( "Running Setup." ) )
            {
                try
                {
                    var binFolder = BinFolder.ReadBinFolder( monitor, binPath );
                    if( binFolder == null ) return false;
                    if( missingImporter != null )
                    {
                        if( !archive.ExtractRuntimeDependencies( new[] { binFolder }, null, missingImporter ) ) return false;
                    }
                    else
                    {
                        if( !archive.ExtractRuntimeDependencies( new[] { binFolder }, remoteStoreUrl, null ) ) return false;
                    }
                    var toSetup = binFolder.Assemblies.Where( b => b.LocalDependencies.Any( dep => dep.ComponentKind == ComponentKind.Model ) )
                                                    .Select( b => b.Name.Name );
                    using( monitor.OpenTrace( "Creating setup configuration xml file." ) )
                    {
                        var config = BuildSetupConfig(
                                        targetConnectionString,
                                        toSetup,
                                        generatedAssemblyName,
                                        sourceGeneration );
                        var configPath = WritDBSetupConfig( monitor, config, binPath );
                        archive.RegisterFileToDelete( configPath );
                    }
                    return RunSetup( monitor, binPath, debugBreakInCKStObjRunner );
                }
                catch( Exception ex )
                {
                    monitor.Fatal( ex );
                    return false;
                }
            }
        }

        static bool RunSetup( IActivityMonitor m, string binPath, bool debugBreakInCKStObjRunner )
        {
            using( m.OpenInfo( "Launching CK.StObj.Runner." ) )
            {
                string exe = Path.Combine( binPath, "CK.StObj.Runner.exe" );
                string dll = Path.Combine( binPath, "CK.StObj.Runner.dll" );
                string fileName, arguments;
                if( !File.Exists( exe ) )
                {
                    if( !File.Exists( dll ) )
                    {
                        m.Error( "Unable to find CK.StObj.Runner runner in folder." );
                        return false;
                    }
                    fileName = "dotnet";
                    arguments = "CK.StObj.Runner.dll merge-deps";
                    if( RunRunnerProcess( m, binPath, debugBreakInCKStObjRunner, fileName, arguments ) )
                    {
                        string theFile = Path.Combine( binPath, "CK.StObj.Runner.deps.json" );
                        File.Replace( theFile + ".merged", theFile, theFile + ".bak" );
                    }
                    arguments = "CK.StObj.Runner.dll";
                }
                else
                {
                    fileName = exe;
                    arguments = String.Empty;
                }
                return RunRunnerProcess( m, binPath, debugBreakInCKStObjRunner, fileName, arguments );
            }
        }

        static bool RunRunnerProcess( IActivityMonitor m, string binPath, bool debugBreakInCKStObjRunner, string fileName, string arguments )
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo();
            cmdStartInfo.WorkingDirectory = binPath;
            cmdStartInfo.RedirectStandardOutput = true;
            cmdStartInfo.RedirectStandardError = true;
            cmdStartInfo.RedirectStandardInput = true;
            cmdStartInfo.UseShellExecute = false;
            cmdStartInfo.CreateNoWindow = true;
            cmdStartInfo.FileName = fileName;
            cmdStartInfo.Arguments = arguments;
            if( debugBreakInCKStObjRunner ) cmdStartInfo.Arguments += " launch-debugger";
            using( Process cmdProcess = new Process() )
            {
                cmdProcess.StartInfo = cmdStartInfo;
                cmdProcess.ErrorDataReceived += ( o, e ) => { if( !string.IsNullOrEmpty( e.Data ) ) m.Error( e.Data ); };
                cmdProcess.OutputDataReceived += ( o, e ) => { if( e.Data != null ) m.Info( e.Data ); };
                cmdProcess.Start();
                cmdProcess.BeginErrorReadLine();
                cmdProcess.BeginOutputReadLine();
                cmdProcess.WaitForExit();
                if( cmdProcess.ExitCode != 0 )
                {
                    m.Error( $"Process returned ExitCode {cmdProcess.ExitCode}." );
                    return false;
                }
                return true;
            }
        }

        static string WritDBSetupConfig( IActivityMonitor m, StObjEngineConfiguration conf, string binPath )
        {
            Func<Type, string> aspectTypeWriter = t =>
             {
                 if( t == typeof( SetupableAspectConfiguration ) ) return "CK.Setup.SetupableAspectConfiguration, CK.Setupable.Model";
                 if( t == typeof( SqlSetupAspectConfiguration ) ) return "CK.Setup.SqlSetupAspectConfiguration, CK.SqlServer.Setup.Model";
                 throw new Exception("Unreachable code.");
             };
            var doc = new XDocument(
                            new XElement( CK.StObj.Runner.Program.xRunner,
                                new XElement( CK.StObj.Runner.Program.xLogFiler, m.MinimalFilter.ToString() ),
                                conf.SerializeXml( new XElement( CK.StObj.Runner.Program.xSetup ), aspectTypeWriter ) ) );
            string filePath = Path.Combine( binPath, CK.StObj.Runner.Program.XmlFileName );
            string text = doc.ToString();
            m.Debug( text );
            File.WriteAllText( filePath, text );
            return filePath;
        }

        static StObjEngineConfiguration BuildSetupConfig(
            string connectionString,
            IEnumerable<string> assembliesToSetup,
            string dynamicAssemblyName,
            bool sourceGeneration )
        {
            var config = new StObjEngineConfiguration();
            config.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.AddRange( assembliesToSetup );
            config.FinalAssemblyConfiguration.AssemblyName = dynamicAssemblyName;
            config.FinalAssemblyConfiguration.SourceGeneration = sourceGeneration;

            var setupable = new SetupableAspectConfiguration();
            config.Aspects.Add( setupable );

            var sql = new SqlSetupAspectConfiguration
            {
                DefaultDatabaseConnectionString = connectionString,
                IgnoreMissingDependencyIsError = true
            };
            config.Aspects.Add( sql );

            return config;
        }

    }
}
