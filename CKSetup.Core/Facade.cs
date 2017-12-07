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

        #region WIP: Run support (no more StObj dependencies).
        /// <summary>
        /// 
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="archive">The opened, valid, runtime archive.</param>
        /// <param name="config">The setup configuration.</param>
        /// <param name="missingImporter">Optional component importer.</param>
        /// <returns>True on success, false on error.</returns>
        public static bool DoRun(
            IActivityMonitor monitor,
            RuntimeArchive archive,
            SetupConfiguration config,
            IComponentImporter missingImporter = null )
        {
            using( monitor.OpenTrace( "Running Setup." ) )
            {
                string workingDir = null;
                try
                {
                    IReadOnlyDictionary<string, BinFileInfo> dedupFiles;
                    IReadOnlyList<BinFolder> folders = ReadBinFolders( monitor, config.BinPaths, out dedupFiles );
                    if( folders == null ) return false;
                    workingDir = GetWorkingDirectory( monitor, config.WorkingDirectory, folders );
                    if( workingDir == null ) return false;
                    var manualDependencies = config.Dependencies.Append( new SetupDependency( "CKSetup.Runner" ) );
                    if( !archive.ExtractRuntimeDependencies( workingDir, folders, missingImporter, manualDependencies ) ) return false;
                    using( monitor.OpenInfo( $"Copying {dedupFiles.Count} files from bin folders." ) )
                    {
                        foreach( var f in dedupFiles.Values )
                        {
                            string targetPath = workingDir + f.LocalFileName;
                            if( File.Exists( targetPath ) )
                            {
                                monitor.Trace( $"Skipping already exisiting '{f.LocalFileName}'." );
                            }
                            else
                            {
                                File.Copy( f.FullPath, targetPath );
                            }
                        }
                    }
                    using( monitor.OpenInfo( $"Generating CKSetup.Runner.Config.xml file." ) )
                    {
                        var root = new XElement( config.Configuration );
                        var ckSetup = new XElement( SetupConfiguration.xCKSetup );
                        ckSetup.Add( new XElement( SetupConfiguration.xEngineAssemblyQualifiedName, config.EngineAssemblyQualifiedName ) );
                        var binPaths = new XElement( SetupConfiguration.xBinPaths );
                        foreach( BinFolder f in folders )
                        {
                            var binPath = new XElement( SetupConfiguration.xBinPath, new XAttribute( SetupConfiguration.xBinPath, f.BinPath ) );
                            var models = f.Assemblies.Where( a => a.ComponentKind == ComponentKind.Model );
                            var dependencies = f.Assemblies.Where( a => a.ComponentKind == ComponentKind.SetupDependency );
                            var modelDependents = f.Assemblies.Where( a => a.ComponentKind == ComponentKind.None && a.LocalDependencies.Any( dep => dep.ComponentKind == ComponentKind.Model ) );
                            binPath.Add( models.Select( a => new XElement( SetupConfiguration.xModel, a.Name.Name ) ) );
                            binPath.Add( dependencies.Select( a => new XElement( SetupConfiguration.xDependency, a.Name.Name ) ) );
                            binPath.Add( modelDependents.Select( a => new XElement( SetupConfiguration.xModelDependent, a.Name.Name ) ) );
                            binPaths.Add( binPath );
                        }
                        ckSetup.Add( binPaths );
                        root.Add( ckSetup );
                        new XDocument( root ).Save( Path.Combine( workingDir, "CKSetup.Runner.Config.xml" ) );
                    }
                    return RunSetupRunner( monitor, workingDir );
                }
                catch( Exception ex )
                {
                    monitor.Fatal( ex );
                    return false;
                }
            }
        }

        static IReadOnlyList<BinFolder> ReadBinFolders(
            IActivityMonitor monitor,
            IEnumerable<string> binPaths,
            out IReadOnlyDictionary<string, BinFileInfo> dedupFiles )
        {
            dedupFiles = null;
            using( monitor.OpenInfo( "Reading binary folders." ) )
            {
                var folders = binPaths.Select( p => BinFolder.ReadBinFolder( monitor, p ) ).ToList();
                if( folders.Any( f => f == null ) ) return null;
                if( folders.Count == 0 )
                {
                    monitor.Error( $"No BinPath provided. At least one is required." );
                    return null;
                }
                using( monitor.OpenInfo( "Checking that common files are the same." ) )
                {
                    bool conflict = false;
                    Dictionary<string, BinFileInfo> byName = new Dictionary<string, BinFileInfo>();
                    foreach( var folder in folders )
                    {
                        foreach( var file in folder.Files )
                        {
                            if( byName.TryGetValue( file.LocalFileName, out var exists ) )
                            {
                                if( exists.ContentSHA1 != file.ContentSHA1 )
                                {
                                    conflict = true;
                                    using( monitor.OpenError( $"File mismatch: {file.LocalFileName}" ) )
                                    {
                                        monitor.Info( $"{exists.BinFolder.BinPath} has {exists.ToString()} with SHA1: {exists.ContentSHA1}." );
                                        monitor.Info( $"{file.BinFolder.BinPath} has {file.ToString()} with SHA1: {file.ContentSHA1}." );
                                    }
                                }
                            }
                            else byName.Add( file.LocalFileName, file );
                        }
                    }
                    if( conflict ) return null;
                    dedupFiles = byName;
                }
                monitor.CloseGroup( $"{folders.Count} folders read." );
                return folders;
            }
        }

        static string GetWorkingDirectory( IActivityMonitor monitor, string workingDir, IReadOnlyList<BinFolder> folders )
        {
            if( !String.IsNullOrWhiteSpace( workingDir ) )
            {
                workingDir = FileUtil.NormalizePathSeparator( Path.GetFullPath( workingDir ), true );
                if( folders.Any( b => b.BinPath.StartsWith( workingDir, StringComparison.OrdinalIgnoreCase ) ) )
                {
                    monitor.Error( $"Working directory can not be inside one of the bin paths." );
                    return null;
                }
                if( Directory.Exists( workingDir ) ) Directory.Delete( workingDir, true );
                Directory.CreateDirectory( workingDir );
            }
            else workingDir = Path.GetTempPath() + Guid.NewGuid().ToString( "N" ) + Path.DirectorySeparatorChar;
            return workingDir;
        }

        static bool RunSetupRunner(
            IActivityMonitor m,
            string workingDir )
        {
            using( m.OpenInfo( "Launching CKSetup.Runner process." ) )
            {
                string exe = Path.Combine( workingDir, "CKSetup.Runner.exe" );
                string dll = Path.Combine( workingDir, "CKSetup.Runner.dll" );
                string fileName, arguments;
                if( !File.Exists( exe ) )
                {
                    if( !File.Exists( dll ) )
                    {
                        m.Error( "Unable to find CKSetup.Runner in folder." );
                        return false;
                    }
                    fileName = "dotnet";
                    #region Using existing runtimeconfig.json to create CKSetup.Runner.runtimeconfig.json.
                    {
                        const string runnerConfigFileName = "CKSetup.Runner.runtimeconfig.json";
                        // We try to find an existing runtimeconfig.json:
                        // - First look for unique runtimeconfig.dev.json
                        //    - It must be unique and have an associated runtimeconfig.json file otherwise we fail.
                        //    - If no dev file exists, we look for a unique runtimeconfig.json
                        // - If there is runtimeconfig.json duplicates in the folder, we fail.
                        // - If there is no runtimeconfig.json, we generate a default one.
                        if( !FindRuntimeConfigFiles( m, workingDir, out string fRtDevPath, out string fRtPath ) )
                        {
                            return false;
                        }
                        string runnerFile = Path.Combine( workingDir, runnerConfigFileName );
                        // If there is a dev file, extracts its additional probe paths.
                        string additionalProbePaths = ExtractJsonAdditonalProbePaths( m, fRtDevPath );
                        if( additionalProbePaths == null )
                        {
                            if( fRtPath == null )
                            {
                                const string defaultRt = "{\"runtimeOptions\":{\"tfm\":\"netcoreapp2.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\": \"2.0.0\"}}}";
                                m.Info( $"Trying with a default {runnerConfigFileName}: {defaultRt}" );
                                File.WriteAllText( runnerFile, defaultRt );
                            }
                            else File.Copy( fRtPath, runnerFile, true );
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
                    }
                    #endregion
                    #region Merging all deps.json into CKSetup.Runner.deps.json
                    {
                        arguments = "CKSetup.Runner.dll merge-deps";
                        if( !RunCKSetupRunnerProcess( m, workingDir, fileName, arguments ) )
                        {
                            return false;
                        }
                        string theFile = Path.Combine( workingDir, "CKSetup.Runner.deps.json" );
                        string theBackup = theFile + ".original";
                        File.Replace( theFile + ".merged", theFile, theBackup );
                    }
                    #endregion
                    arguments = "CKSetup.Runner.dll ";
                }
                else
                {
                    fileName = exe;
                    arguments = String.Empty;
                }
                return RunCKSetupRunnerProcess( m, workingDir, fileName, arguments );
            }
        }

        static bool RunCKSetupRunnerProcess(
                IActivityMonitor m,
                string workingDir,
                string fileName,
                string arguments )
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo();
            cmdStartInfo.WorkingDirectory = workingDir;
            cmdStartInfo.UseShellExecute = false;
            cmdStartInfo.CreateNoWindow = true;
            cmdStartInfo.FileName = fileName;
            using( var logReceiver = LogReceiver.Start( m, true ) )
            {
                cmdStartInfo.Arguments = arguments;
                cmdStartInfo.Arguments += " /logPipe:" + logReceiver.PipeName;
                using( m.OpenTrace( $"{fileName} {cmdStartInfo.Arguments}" ) )
                using( Process cmdProcess = new Process() )
                {
                    cmdProcess.StartInfo = cmdStartInfo;
                    cmdProcess.Start();
                    cmdProcess.WaitForExit();
                    var endLogStatus = logReceiver.WaitEnd( cmdProcess.ExitCode != 0 );
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

        #endregion

        /// <summary>
        /// This is static in order to ease tests.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="binPath">The bin path to setup.</param>
        /// <param name="archive">The opened, valid, Zip runtime.</param>
        /// <param name="targetConnectionString">The default connection string.</param>
        /// <param name="generatedAssemblyName">Name of the assembly to generate.</param>
        /// <param name="runnerLogFilter">Log filter that the runner must use.</param>
        /// <param name="missingImporter">Optional component importer.</param>
        /// <param name="remoteStoreUrl">Optional remote store url (ignored if a <paramref name="missingImporter"/> is specified).</param>
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
            LogFilter runnerLogFilter,
            IComponentImporter missingImporter = null,
            Uri remoteStoreUrl = null,
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
                        if( binFolder.BinPath.StartsWith( keepFolder.FullName ) )
                        {
                            monitor.Error( "KeepFolder must not be in bin path!" );
                            return false;
                        }
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
                                        generatedAssemblyName  );
                        var configPath = WritDBSetupConfig( monitor, config, binPath, runnerLogFilter );
                        if( keepFolder != null )
                        {
                            File.Copy( configPath, Path.Combine( keepFolder.FullName, CK.StObj.Runner.Program.XmlFileName ) );
                        }
                        archive.RegisterFileToDelete( configPath );
                    }
                    return RunSetup( monitor, binPath, archive, keepFolder );
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
            DirectoryInfo keepFolder )
        {
            using( m.OpenInfo( "Launching CK.StObj.Runner process." ) )
            {
                string exe = Path.Combine( binPath, "CK.StObj.Runner.exe" );
                string dll = Path.Combine( binPath, "CK.StObj.Runner.dll" );
                string fileName, arguments;
                bool usePipeLogs;
                if( !File.Exists( exe ) )
                {
                    if( !File.Exists( dll ) )
                    {
                        m.Error( "Unable to find CK.StObj.Runner runner in folder." );
                        return false;
                    }
                    usePipeLogs = GetUsePipeLogFromRunnerVersion( m, dll );

                    fileName = "dotnet";
                    #region Using existing runtimeconfig.json to create CK.StObj.Runner.runtimeconfig.json.
                    {
                        const string runnerConfigFileName = "CK.StObj.Runner.runtimeconfig.json";
                        // We try to find an existing runtimeconfig.json:
                        // - First look for unique runtimeconfig.dev.json
                        //    - It must be unique and have an associated runtimeconfig.json file otherwise we fail.
                        //    - If no dev file exists, we look for a unique runtimeconfig.json
                        // - If there is runtimeconfig.json duplicates in the folder, we fail.
                        // - If there is no runtimeconfig.json, we generate a default one.
                        if( !FindRuntimeConfigFiles( m, binPath, out string fRtDevPath, out string fRtPath ) )
                        {
                            return false;
                        }
                        string runnerFile = Path.Combine( binPath, runnerConfigFileName );
                        // If there is a dev file, extracts its additional probe paths.
                        string additionalProbePaths = ExtractJsonAdditonalProbePaths( m, fRtDevPath );
                        if( additionalProbePaths == null )
                        {
                            if( fRtPath == null )
                            {
                                const string defaultRt = "{\"runtimeOptions\":{\"tfm\":\"netcoreapp2.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\": \"2.0.0\"}}}";
                                m.Info( $"Trying with a default {runnerConfigFileName}: {defaultRt}" );
                                File.WriteAllText( runnerFile, defaultRt );
                            }
                            else File.Copy( fRtPath, runnerFile, true );
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
                        if( !RunRunnerProcess( m, usePipeLogs, binPath, fileName, arguments ) )
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
                    usePipeLogs = GetUsePipeLogFromRunnerVersion( m, exe );
                }
                return RunRunnerProcess( m, usePipeLogs, binPath, fileName, arguments );
            }
        }

        private static bool GetUsePipeLogFromRunnerVersion( IActivityMonitor m, string runnerPath )
        {
            bool usePipeLogs;
            var vRunner = new CSemVer.InformationalVersion( FileVersionInfo.GetVersionInfo( runnerPath ).ProductVersion );
            m.Info( $"Using CK.StObj.Runner version = {vRunner.SemVersion ?? CSemVer.SVersion.ZeroVersion}" );
            usePipeLogs = vRunner.SemVersion == null || vRunner.SemVersion.CompareTo( new CSemVer.SVersion( 5, 0, 0, "delta.3" ) ) > 0;
            return usePipeLogs;
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
                    // When there is NO runtimeconfig.json file, this is not an error:
                    // We'll use a default one (and pray).
                    m.Warn( $"Unable to find a runtimeconfig.json file." );
                    return true;
                }
                fRtPath = rtFiles[0];
            }
            return true;
        }

        static bool RunRunnerProcess(
                        IActivityMonitor m,
                        bool usePipeLogs,
                        string binPath,
                        string fileName,
                        string arguments )
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo();
            cmdStartInfo.WorkingDirectory = binPath;
            if( !usePipeLogs )
            {
                cmdStartInfo.RedirectStandardOutput = true;
                cmdStartInfo.RedirectStandardError = true;
                cmdStartInfo.RedirectStandardInput = true;
            }
            cmdStartInfo.UseShellExecute = false;
            cmdStartInfo.CreateNoWindow = true;
            cmdStartInfo.FileName = fileName;
            using( var logReceiver = usePipeLogs ? LogReceiver.Start( m, true ) : null )
            {
                cmdStartInfo.Arguments = arguments;
                if( usePipeLogs ) cmdStartInfo.Arguments += " /logPipe:" + logReceiver.PipeName;
                using( m.OpenTrace( $"{fileName} {cmdStartInfo.Arguments}" ) )
                using( Process cmdProcess = new Process() )
                {
                    cmdProcess.StartInfo = cmdStartInfo;
                    if( !usePipeLogs )
                    {
                        cmdProcess.ErrorDataReceived += ( o, e ) => { if( !string.IsNullOrEmpty( e.Data ) ) m.Error( e.Data ); };
                        cmdProcess.OutputDataReceived += ( o, e ) => { if( e.Data != null ) m.Info( e.Data ); };
                    }
                    cmdProcess.Start();
                    if( !usePipeLogs )
                    {
                        cmdProcess.BeginErrorReadLine();
                        cmdProcess.BeginOutputReadLine();
                    }
                    cmdProcess.WaitForExit();
                    if( usePipeLogs )
                    {
                        var endLogStatus = logReceiver.WaitEnd( cmdProcess.ExitCode != 0 );
                        if( endLogStatus != LogReceiverEndStatus.Normal )
                        {
                            m.Warn( $"Pipe log channel abnormal end status: {endLogStatus}." );
                        }
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
            string dynamicAssemblyName )
        {
            var config = new StObjEngineConfiguration();
            config.Assemblies.AddRange( assembliesToSetup );
            config.GeneratedAssemblyName = dynamicAssemblyName;

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
