using CK.Core;
using CK.Setup;
using CK.Text;
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
        /// <param name="runnerLogFilter">Log filter that the runner must use.</param>
        /// <param name="missingImporter">Optional component importer.</param>
        /// <param name="remoteStoreUrl">Optional remote store url (ignored if a <paramref name="missingImporter"/> is specified).</param>
        /// <param name="debugBreakInCKStObjRunner">Calls Debugger.Launch() at the start of CK.StObj.Runner entry point.</param>
        /// <param name="keepRuntimesFilesFolder">
        /// Optional root path or path relative to the <paramref name="binPath"/> that will be cleaned up and
        /// filled with a copy of all the runtime files that have been resolved and injected along
        /// with a FilesSkippedSinceTheyExist.txt file. 
        /// </param>
        /// <returns>True on success, false on error.</returns>
        public static bool DoSetup(
            IActivityMonitor monitor,
            string binPath,
            RuntimeArchive archive,
            string targetConnectionString,
            string generatedAssemblyName,
            bool sourceGeneration,
            LogFilter runnerLogFilter,
            IComponentImporter missingImporter = null,
            Uri remoteStoreUrl = null,
            bool debugBreakInCKStObjRunner = false,
            string keepRuntimesFilesFolder = null )
        {
            using( monitor.OpenTrace( "Running Setup." ) )
            {
                try
                {
                    var binFolder = BinFolder.ReadBinFolder( monitor, binPath );
                    if( binFolder == null ) return false;
                    DirectoryInfo keepFolder = null;
                    if( keepRuntimesFilesFolder != null )
                    {
                        if( !Path.IsPathRooted( keepRuntimesFilesFolder ) )
                        {
                            keepRuntimesFilesFolder = Path.Combine( binPath, keepRuntimesFilesFolder );
                        }
                        keepFolder = new DirectoryInfo( Path.GetFullPath( keepRuntimesFilesFolder ) );
                    }
                    if( missingImporter != null )
                    {
                        if( !archive.ExtractRuntimeDependencies( new[] { binFolder }, null, missingImporter, keepFolder ) ) return false;
                    }
                    else
                    {
                        if( !archive.ExtractRuntimeDependencies( new[] { binFolder }, remoteStoreUrl, null, keepFolder ) ) return false;
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
                        var configPath = WritDBSetupConfig( monitor, config, binPath, runnerLogFilter );
                        if( keepFolder != null )
                        {
                            File.Copy( configPath, Path.Combine( keepFolder.FullName, CK.StObj.Runner.Program.XmlFileName ) );
                        }
                        archive.RegisterFileToDelete( configPath );
                    }
                    return RunSetup( monitor, binPath, archive, keepFolder, debugBreakInCKStObjRunner );
                }
                catch( Exception ex )
                {
                    monitor.Fatal( ex );
                    return false;
                }
            }
        }

        static bool RunSetup(
            IActivityMonitor m,
            string binPath,
            RuntimeArchive archive,
            DirectoryInfo keepFolder,
            bool debugBreakInCKStObjRunner )
        {
            using( m.OpenInfo( "Launching CK.StObj.Runner process." ) )
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
                    #region Using existing runtimeconfig.json to create CK.StObj.Runner.runtimeconfig.json.
                    {
                        const string runnerConfigFileName = "CK.StObj.Runner.runtimeconfig.json";
                        if( !FindRuntimeConfigFiles( m, binPath, out string fRtDevPath, out string fRtPath ) )
                        {
                            return false;
                        }
                        string runnerFile = Path.Combine( binPath, runnerConfigFileName );
                        string additionalProbePaths = ExtractJsonAdditonalProbePaths( m, fRtDevPath );
                        if( additionalProbePaths == null )
                        {
                            File.Copy( fRtPath, runnerFile, true );
                        }
                        else
                        {
                            string txt = File.ReadAllText( fRtPath );
                            int idx = txt.LastIndexOf( '}' ) - 1;
                            if( idx > 0 ) idx = txt.LastIndexOf( '}', idx ) - 1;
                            if( idx > 0 )
                            {
                                txt = txt.Insert( idx, "," + additionalProbePaths );
                                File.WriteAllText( runnerFile, txt );
                            }
                            else
                            {
                                using( m.OpenError( "Unable to inject additionalProbingPaths in:" ) )
                                {
                                    m.Error( txt );
                                }
                                return false;
                            }
                        }
                        if( keepFolder != null )
                        {
                            File.Copy( runnerFile, Path.Combine( keepFolder.FullName, runnerConfigFileName ) );
                        }
                        archive.RegisterFileToDelete( runnerFile );
                    }
                    #endregion
                    #region Merging all deps.json into CK.StObj.Runner.deps.json
                    {
                        arguments = "CK.StObj.Runner.dll merge-deps";
                        if( !RunRunnerProcess( m, binPath, debugBreakInCKStObjRunner, fileName, arguments ) )
                        {
                            return false;
                        }
                        string theFile = Path.Combine( binPath, "CK.StObj.Runner.deps.json" );
                        string theBackup = theFile + ".cksetup-backup";
                        File.Replace( theFile + ".merged", theFile, theBackup );
                        archive.RegisterFileToDelete( theBackup );
                    }
                    #endregion
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

        private static string ExtractJsonAdditonalProbePaths( IActivityMonitor m, string fRtDevPath )
        {
            if( fRtDevPath == null ) return null;
            string txt = File.ReadAllText( fRtDevPath );
            var matcher = new StringMatcher( txt );
            List<KeyValuePair<string, object>> oRoot;
            int idxRuntimeOptions;
            List<KeyValuePair<string, object>> oRuntimeOptions;
            int idxAdditionalProbingPaths;
            List<object> additionalProbingPathsObj;
            if( matcher.MatchJSONObject( out object oConfig )
                && (oRoot = (oConfig as List<KeyValuePair<string, object>>)) != null
                && (idxRuntimeOptions = oRoot.IndexOf( kv => kv.Key == "runtimeOptions" )) >= 0
                && (oRuntimeOptions = oRoot[idxRuntimeOptions].Value as List<KeyValuePair<string, object>>) != null
                && (idxAdditionalProbingPaths = oRuntimeOptions.IndexOf( kv => kv.Key == "additionalProbingPaths" )) >= 0
                && (additionalProbingPathsObj = oRuntimeOptions[idxAdditionalProbingPaths].Value as List<object>) != null
                && additionalProbingPathsObj.All( p => p is string ) )
            {
                m.Trace( $"Extracted {additionalProbingPathsObj.OfType<string>().Concatenate()} from {fRtDevPath}." );
                return "\"additionalProbingPaths\":[\""
                            + additionalProbingPathsObj
                                .Select( p => ((string)p).Replace("\\","\\\\" ) )
                                .Concatenate( "\", \"" )
                            + "\"]";
            }
            using( m.OpenWarn( "Unable to extract any additional probing paths from:" ) )
            {
                m.Warn( txt );
            }
            return null;
        }

        private static bool FindRuntimeConfigFiles( IActivityMonitor m, string binPath, out string fRtDevPath, out string fRtPath )
        {
            fRtDevPath = fRtPath = null;
            // First find the dev file.
            var devs = Directory.EnumerateFiles( binPath, "*.runtimeconfig.dev.json" ).ToList();
            if( devs.Count > 1 )
            {
                m.Error( $"Found more than one runtimeconfig.dev.json files: '{String.Join( "', '", devs.Select( p => Path.GetFileName( p ) ) ) }'." );
                return false;
            }
            if( devs.Count == 1 )
            {
                fRtDevPath = devs[0];
                string fRtDevName = Path.GetFileName( fRtDevPath );
                m.Trace( $"Found '{fRtDevName}'." );
                string fRtName = fRtDevName.Remove( fRtDevName.Length - 9, 4 );
                fRtPath = Path.Combine( binPath, fRtName );
                if( !File.Exists( fRtPath ) )
                {
                    m.Error( $"Unable to find '{fRtName}' file (but found '{fRtDevName}')." );
                    return false;
                }
            }
            else
            {
                var rtFiles = Directory.EnumerateFiles( binPath, "*.runtimeconfig.json" ).ToList();
                if( rtFiles.Count > 1 )
                {
                    m.Error( $"Found more than one runtimeconfig.json files: '{String.Join( "', '", rtFiles.Select( p => Path.GetFileName( p ) ) ) }'." );
                    return false;
                }
                else if( rtFiles.Count == 0 )
                {
                    m.Error( $"Unable to find a runtimeconfig.json file." );
                    return false;
                }
                fRtPath = rtFiles[0];
            }
            return true;
        }

        static bool RunRunnerProcess( IActivityMonitor m, string binPath, bool debugBreakInCKStObjRunner, string fileName, string arguments )
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo();
            cmdStartInfo.WorkingDirectory = binPath;
            cmdStartInfo.UseShellExecute = false;
            cmdStartInfo.CreateNoWindow = true;
            cmdStartInfo.FileName = fileName;
            using( var logReceiver = LogReceiver.Start( m, true ) )
            {
                cmdStartInfo.Arguments = arguments + " /logPipe:" + logReceiver.PipeName;
                if( debugBreakInCKStObjRunner ) cmdStartInfo.Arguments += " launch-debugger";
                using( m.OpenTrace( $"{fileName} {cmdStartInfo.Arguments}" ) )
                using( Process cmdProcess = new Process() )
                {
                    cmdProcess.StartInfo = cmdStartInfo;
                    cmdProcess.Start();
                    cmdProcess.WaitForExit();
                    var endLogStatus = logReceiver.WaitEnd();
                    if( endLogStatus != LogReceiverEndStatus.Normal )
                    {
                        m.Warn( $"Pipe log channel abnormal end status: {endLogStatus}." );
                    }
                    if( cmdProcess.ExitCode != 0 )
                    {
                        m.Error( $"Process returned ExitCode {cmdProcess.ExitCode}." );
                        return false;
                    }
                    return true;
                }
            }
        }

        static string WritDBSetupConfig( IActivityMonitor m, StObjEngineConfiguration conf, string binPath, LogFilter logFilter )
        {
            Func<Type, string> aspectTypeWriter = t =>
             {
                 if( t == typeof( SetupableAspectConfiguration ) ) return "CK.Setup.SetupableAspectConfiguration, CK.Setupable.Model";
                 if( t == typeof( SqlSetupAspectConfiguration ) ) return "CK.Setup.SqlSetupAspectConfiguration, CK.SqlServer.Setup.Model";
                 throw new Exception("Unreachable code.");
             };
            var doc = new XDocument(
                            new XElement( CK.StObj.Runner.Program.xRunner,
                                new XElement( CK.StObj.Runner.Program.xLogFiler, logFilter.ToString() ),
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
