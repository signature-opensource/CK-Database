using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using System.Xml.Linq;
using CSemVer;
using CK.Text;
using System.Diagnostics;
using System.IO;
using CKSetup.StreamStore;

namespace CKSetup
{
    public class RuntimeArchive : IDisposable
    {
        public const string DbXmlFileName = "db.xml";
        readonly IStreamStore _store;
        readonly List<string> _cleanupFiles;
        readonly IActivityMonitor _monitor;
        readonly CompressionKind _storageKind;
        ComponentDB _dbCurrent;
        ComponentDB _dbOrigin;

        public RuntimeArchive( IActivityMonitor monitor, IStreamStore store, CompressionKind storageKind )
        {
            _store = store;
            _cleanupFiles = new List<string>();
            _monitor = monitor;
            _storageKind = storageKind;
            _dbOrigin = _dbCurrent = _store.Initialize( monitor );
            if( _dbOrigin == null )
            {
                _store.Dispose();
            }
        }

        /// <summary>
        /// Gets whether this database is has been successfully opened.
        /// </summary>
        public bool IsValid => _dbCurrent != null;

        /// <summary>
        /// Registers a file that will be automatically deleted when this <see cref="RuntimeArchive"/>
        /// will be disposed.
        /// </summary>
        /// <param name="fullPath"></param>
        public void RegisterFileToDelete( string fullPath )
        {
            _cleanupFiles.Add( fullPath );
        }

        /// <summary>
        /// Removes all registered components.
        /// </summary>
        /// <returns>True on success, false on error.</returns>
        public bool Clear()
        {
            using( _monitor.OpenInfo( $"Clearing runtime archive." ) )
            {
                try
                {
                    _dbCurrent = new ComponentDB();
                    int deleted = _store.Delete( f => !f.Equals( DbXmlFileName, StringComparison.OrdinalIgnoreCase ) );
                    _monitor.CloseGroup( $"{deleted} entries removed." );
                    SaveDbCurrent();
                    return true;
                }
                catch( Exception ex )
                {
                    _monitor.Error( ex );
                    return false;
                }
            }
        }

        /// <summary>
        /// Determines whether a component is registered.
        /// </summary>
        /// <param name="n">Component name.</param>
        /// <param name="t">Target framework of the component.</param>
        /// <param name="v">Version of the component.</param>
        public bool Contains( string n, TargetFramework t, SVersion v )
        {
            return _dbCurrent.Find( new ComponentRef( n, t, v ) ) != null;
        }

        public class LocalImporter
        {
            readonly RuntimeArchive _archive;
            readonly HashSet<BinFolder> _toAdd;
            readonly IComponentImporter _missingImporter;

            internal LocalImporter( RuntimeArchive a, IComponentImporter missingImporter )
            {
                _archive = a;
                _toAdd = new HashSet<BinFolder>();
                _missingImporter = missingImporter;
            }

            IActivityMonitor Monitor => _archive._monitor;

            /// <summary>
            /// Adds one or more components. Ignores them when already registered.
            /// </summary>
            /// <param name="folder">The (potentially multiples) component folder.</param>
            /// <returns>This importer (enable fluent syntax).</returns>
            public LocalImporter AddComponent( params BinFolder[] folder ) => AddComponent( (IEnumerable<BinFolder>)folder );

            /// <summary>
            /// Adds one or more components. Ignores them when already registered.
            /// </summary>
            /// <param name="folders">Folders to add. Must not contain null folders.</param>
            /// <returns>This importer (enable fluent syntax).</returns>
            public LocalImporter AddComponent( IEnumerable<BinFolder> folders )
            {
                if( folders == null ) throw new ArgumentNullException( nameof( folders ) );
                if( folders.Any( f => f == null ) ) throw new ArgumentNullException( nameof( folders ), $"{nameof(folders)}.ElementAt({folders.IndexOf( f => f ==  null )}) is null." );
                _toAdd.AddRange( folders.Where( f => f != null ) );
                return this;
            }

            /// <summary>
            /// Tries to import added component.
            /// </summary>
            /// <returns>True on success, false on error.</returns>
            public bool Import()
            {
                if( _toAdd.Count == 0 ) return true;
                // Small trick: ordering by files count will tend to reduce
                // Components mutation and multiple heads registration errors
                // since smallest (ie. most basic) components come first.
                return _archive.LocalImport( _toAdd.OrderBy( f => f.Files.Count ), _missingImporter );
            }
        }

        /// <summary>
        /// Creates a local importer object that can be used to import local <see cref="BinFolder"/>.
        /// </summary>
        /// <returns>An importer.</returns>
        public LocalImporter CreateLocalImporter( IComponentImporter missingImporter = null )
        {
            if( !IsValid ) throw new InvalidOperationException();
            return new LocalImporter( this, missingImporter );
        }

        bool LocalImport( IOrderedEnumerable<BinFolder> folders, IComponentImporter missingImporter )
        {
            if( !IsValid ) throw new InvalidOperationException();
            using( _monitor.OpenInfo( $"Importing local folders." ) )
            {
                ComponentDB db = _dbCurrent;
                var added = new Dictionary<ComponentRef, BinFolder>();
                foreach( var f in folders )
                {
                    using( _monitor.OpenInfo( $"Adding {f.BinPath}." ) )
                    {
                        var result = db.AddLocal( _monitor, f );
                        if( result.Error ) return false;
                        // We do not store files for models: they are necessarily in 
                        // any target folders that has the model!
                        if( result.NewComponent != null && result.NewComponent.ComponentKind != ComponentKind.Model )
                        {
                            added[result.NewComponent.GetRef()] = f;
                        }
                        db = result.NewDB;
                    }
                }
                var newC = added.Select( kv => new { C = db.Components.Single( c => c.GetRef().Equals( kv.Key ) ), F = kv.Value } );
                if( newC.Any( c => c.C.Embedded.Count > 0 ) )
                {
                    if( missingImporter != null )
                    {
                        using( _monitor.OpenInfo( $"Components have missing embedded components. Trying to use importer." ) )
                        {
                            var downloader = new ComponentDownloader( missingImporter, db, _store, _storageKind );
                            var missing = new ComponentMissingDescription( newC.SelectMany( c => c.C.Embedded ).ToList() );
                            var updatedDB = downloader.Download( _monitor, missing );
                            if( updatedDB == null ) return false;
                            db = updatedDB;
                        }
                    }
                    else
                    {
                        using( _monitor.OpenError( "Components have missing embedded components. Embedded components are required to be resolved." ) )
                        {
                            foreach( var c in newC.Where( c => c.C.Embedded.Count > 0 ) )
                            {
                                _monitor.Trace( $"'{c.C}' embedds: '{c.C.Embedded.Select( e => e.ToString() ).Concatenate( "', '" )}'." );
                            }
                        }
                        return false;
                    }
                }
                foreach( var c in newC )
                {
                    foreach( var f in c.C.Files )
                    {
                        using( _monitor.OpenTrace( $"Importing file {f.Name} content." ) )
                        {
                            try
                            {
                                string key = f.SHA1.ToString();
                                if( _store.Exists( key ) )
                                {
                                    _monitor.CloseGroup( "Already stored." );
                                }
                                else
                                {
                                    using( var content = File.OpenRead( c.F.Files.Single( b => b.ContentSHA1 == f.SHA1 ).FullPath ) )
                                    {
                                        _store.Create( key, content, CompressionKind.None, _storageKind );
                                    }
                                }
                            }
                            catch( Exception ex)
                            {
                                _monitor.Error( ex );
                                return false;
                            }
                        }
                    }
                }
                _dbCurrent = db;
                SaveDbCurrent();
                return true;
            }
        }

        class ComponentDownloader : IComponentDownloader
        {
            readonly IComponentImporter _importer;
            readonly IStreamStore _store;
            readonly CompressionKind _storageKind;
            ComponentDB _db;

            public ComponentDownloader( IComponentImporter importer, RuntimeArchive a )
                : this( importer, a._dbCurrent, a._store, a._storageKind )
            {
            }

            public ComponentDownloader( IComponentImporter importer, ComponentDB db, IStreamStore store, CompressionKind storageKind )
            {
                _importer = importer;
                _store = store;
                _storageKind = storageKind;
                _db = db;
            }

            public ComponentDB Download( IActivityMonitor monitor, ComponentMissingDescription missing )
            {
                using( var s = _importer.OpenImportStream( monitor, missing ) )
                {
                    if( s == null || !ImportComponents( monitor, s, _importer ) ) return null;
                    return _db;
                }
            }

            bool ImportComponents( IActivityMonitor monitor, Stream input, IComponentFileDownloader downloader )
            {
                using( monitor.OpenInfo( "Starting import." ) )
                {
                    var n = _db.Import( monitor, input );
                    if( n.Error ) return false;
                    var r = _store.DownloadImportResult( monitor, downloader, n, _storageKind );
                    if( r.Item2 > 0 ) return false;
                    _db = n.NewDB;
                    return true;
                }
            }
        }

        /// <summary>
        /// Extracts required runtime support for Models in a targets.
        /// </summary>
        /// <param name="targets">The target folders.</param>
        /// <param name="remoteUrl">Remote store url. Can be null.</param>
        /// <param name="runPath">Optional run path: the first target <see cref="BinFolder.BinPath"/> is the default.</param>
        /// <returns>True on success, false on error.</returns>
        public bool ExtractRuntimeDependencies( IEnumerable<BinFolder> targets, Uri remoteUrl, string runPath = null )
        {
            using( var store = remoteUrl != null ? new ClientRemoteStore( remoteUrl, null ) : null )
            {
                return ExtractRuntimeDependencies( targets, runPath, store );
            }
        }

        /// <summary>
        /// Extracts required runtime support for Models in a targets.
        /// </summary>
        /// <param name="targets">The target folders.</param>
        /// <param name="runPath">Optional run path: the first target <see cref="BinFolder.BinPath"/> is the default.</param>
        /// <returns>True on success, false on error.</returns>
        public bool ExtractRuntimeDependencies( IEnumerable<BinFolder> targets, string runPath = null, IComponentImporter missingImporter = null )
        {
            if( !targets.Any() ) throw new ArgumentException( "At least one target is required.", nameof( targets ) );
            using( _monitor.OpenInfo( $"Extracting runtime support for '{targets.Select( t => t.BinPath ).Concatenate()}'." ) )
            {
                if( runPath == null ) runPath = targets.First().BinPath;
                _monitor.Info( $"Extracting to {runPath}." );
                var resolver = _dbCurrent.GetRuntimeDependenciesResolver( _monitor, targets );
                if( resolver == null ) return false;
                IComponentDownloader downloader = missingImporter != null
                                                    ? new ComponentDownloader(missingImporter, this)
                                                    : null;
                IReadOnlyList<Component> components = resolver.Run( _monitor, downloader );
                if( components == null ) return false;
                int count = 0;
                foreach( var c in components )
                {
                    using( _monitor.OpenInfo( $"{c.Files.Count} files from '{c}'." ) )
                    {
                        foreach( var f in c.Files )
                        {
                            string targetPath = runPath + f.Name;
                            if( !File.Exists( targetPath ) )
                            {
                                try
                                {
                                    string fileKey = f.SHA1.ToString();
                                    if( !_store.Exists(fileKey) )
                                    {
                                        if( missingImporter == null )
                                        {
                                            _monitor.Error( $"Missing file '{f}' in local store." );
                                            return false;
                                        }
                                        if( !_store.Download( _monitor, missingImporter, f, _storageKind ) ) return false;
                                    }
                                    _store.ExtractToFile( fileKey, targetPath );
                                    _monitor.Debug( $"Extracted {f.Name}." );
                                    _cleanupFiles.Add( targetPath );
                                    ++count;
                                }
                                catch( Exception ex )
                                {
                                    _monitor.Error( $"While extracting '{targetPath}'.", ex );
                                    return false;
                                }
                            }
                            else _monitor.Trace( $"Skipped '{targetPath}' since it already exists." );
                        }
                    }
                    _monitor.Info( $"{count} files extracted." );
                }
            }
            return true;
        }

        /// <summary>
        /// Exports a filtered set of components to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="filter">Filter for components to export.</param>
        /// <param name="output">Output stream.</param>
        public void Export( Func<Component, bool> filter, Stream output )
        {
            _dbCurrent.Export( filter, output );
        }

        /// <summary>
        /// Exports available components.
        /// </summary>
        /// <param name="what">Required description.</param>
        /// <param name="output">Output stream.</param>
        /// <param name="monitor">Optional monitor to use.</param>
        public void ExportComponents(
            ComponentMissingDescription what,
            Stream output,
            IActivityMonitor monitor = null )
        {
            var content = _dbCurrent.FindAvailable( what, monitor );
            Export( c => content.Contains( c ), output );
        }

        /// <summary>
        /// Imports a set of components from a <see cref="Stream"/> and a downloader.
        /// </summary>
        /// <param name="input">Input stream.</param>
        /// <param name="downloader">Missing files downloader.</param>
        /// <returns>True on success, false on error.</returns>
        public bool ImportComponents( Stream input, IComponentFileDownloader downloader )
        {
            if( downloader == null ) throw new ArgumentNullException( nameof( downloader ) );
            using( _monitor.OpenInfo( "Starting import with file downloader." ) )
            {
                var n = _dbCurrent.Import( _monitor, input );
                if( n.Error ) return false;
                var r = _store.DownloadImportResult( _monitor, downloader, n, _storageKind );
                if( r.Item2 > 0 )
                {
                    _monitor.Error( $"{r.Item2} download errors. Import canceled." );
                    return false;
                }
                _dbCurrent = n.NewDB;
                SaveDbCurrent();
                return true;
            }
        }

        /// <summary>
        /// Imports a set of components from a <see cref="Stream"/> and returns
        /// a <see cref="PushComponentsResult"/>.
        /// </summary>
        /// <param name="input">Input stream.</param>
        /// <param name="sessionId">
        /// Optional session identifier.
        /// When not set, <see cref="PushComponentsResult.SessionId"/> is null on error
        /// and a new guid is generated on success.
        /// </param>
        /// <returns>True on success, false on error.</returns>
        public PushComponentsResult ImportComponents( Stream input, string sessionId = null )
        {
            using( _monitor.OpenInfo( "Starting import." ) )
            {
                var n = _dbCurrent.Import( _monitor, input );
                if( n.Error ) return new PushComponentsResult("Error while importing component into ComponentDB.", sessionId );
                IReadOnlyList<SHA1Value> missingFiles;
                if( n.Components != null && n.Components.Count > 0 )
                {
                    missingFiles = n.Components
                                    .Where( c => c.ComponentKind != ComponentKind.Model )
                                    .SelectMany( c => c.Files )
                                    .Select( f => f.SHA1 )
                                    .Distinct()
                                    .Where( sha => !_store.Exists( sha.ToString() ) )
                                    .ToList();
                }
                else missingFiles = Array.Empty<SHA1Value>();
                _dbCurrent = n.NewDB;
                SaveDbCurrent();
                return new PushComponentsResult( missingFiles, sessionId ?? Guid.NewGuid().ToString() );
            }
        }

        /// <summary>
        /// Pushes selected components to a <see cref="IComponentPushTarget"/>.
        /// </summary>
        /// <param name="filter">Filter for components to export.</param>
        /// <param name="target">Target for the components.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool PushComponents( Func<Component,bool> filter, IComponentPushTarget target )
        {
            if( filter == null ) throw new ArgumentNullException( nameof( filter ) );
            if( target == null ) throw new ArgumentNullException( nameof( target ) );

            bool fileError = false;
            using( _monitor.OpenInfo( $"Starting component push." ) )
            {
                var result = target.PushComponents( _monitor, w => _dbCurrent.Export( filter, w ) );
                if( result.ErrorText != null )
                {
                    _monitor.Error( "Target error: " + result.ErrorText );
                    return false;
                }
                int fileCount = 0;
                if( result.Files.Count > 0 )
                {
                    using( _monitor.OpenInfo( $"Starting {result.Files.Count} push. SessionId={result.SessionId}." ) )
                    {
                        foreach( var sha in result.Files )
                        {
                            ++fileCount;
                            StoredStream sf = _store.OpenRead( sha.ToString() );
                            if( sf.Stream != null )
                            {
                                try
                                {
                                    if( !target.PushFile( _monitor, result.SessionId, sha, w => sf.Stream.CopyTo( w ), sf.Kind ) )
                                    {
                                        _monitor.Error( $"Failed to push file {sha}." );
                                        --fileCount;
                                        fileError = true;
                                    }
                                }
                                finally
                                {
                                    sf.Stream.Dispose();
                                }
                            }
                            else
                            {
                                _monitor.Warn( $"Target requested file '{sha}' that does not locally exist." );
                                --fileCount;
                            }
                        }
                    }
                }
                if( !fileError ) _monitor.CloseGroup( $"Target is up to date. {fileCount} file uploaded." );
            }
            return !fileError;
        }


        /// <summary>
        /// Pushes selected components to a remote url.
        /// </summary>
        /// <param name="filter">Filter for components to export.</param>
        /// <param name="url">Url of the remote.</param>
        /// <param name="apiKey">Optional api key.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool PushComponents( Func<Component, bool> filter, Uri url, string apiKey )
        {
            if( filter == null ) throw new ArgumentNullException( nameof( filter ) );
            if( url == null ) throw new ArgumentNullException( nameof( url ) );
            using( var remote = new ClientRemoteStore( url, apiKey ) )
            {
                return PushComponents( filter, remote );
            }
        }

        void SaveDbCurrent()
        {
            if( _dbOrigin != _dbCurrent )
            {
                _store.Save( _dbCurrent );
                _dbOrigin = _dbCurrent;
            }
        }

        /// <summary>
        /// Closes this archive. Files registered by <see cref="RegisterFileToDelete"/> 
        /// are deleted and updated component database is written in the zip if required.
        /// </summary>
        public void Dispose()
        {
            if( _dbCurrent == null ) return;
            using( _monitor.OpenInfo( "Closing Zip archive." ) )
            {
                if( _cleanupFiles.Count > 0 )
                {
                    using( _monitor.OpenTrace( $"Cleaning {_cleanupFiles.Count} runtime files." ) )
                    {
                        foreach( var f in _cleanupFiles )
                        {
                            try
                            {
                                File.Delete( f );
                            }
                            catch( Exception ex )
                            {
                                _monitor.Warn( ex );
                            }
                        }
                        _cleanupFiles.Clear();
                    }
                }
                SaveDbCurrent();
                _store.Dispose();
                _dbCurrent = null;
            }
        }


        /// <summary>
        /// Opens an existing archive or creates a new one.
        /// </summary>
        /// <param name="m">Monitor to use. Can not be null.</param>
        /// <param name="path">Path to the file to open or create.</param>
        /// <returns>An archive or null on error.</returns>
        static public RuntimeArchive OpenOrCreate( IActivityMonitor m, string path )
        {
            try
            {
                IStreamStore store;
                CompressionKind kind;
                if( path.EndsWith( ".zip", StringComparison.OrdinalIgnoreCase ) )
                {
                    store = new ZipFileStreamStore( path );
                    kind = CompressionKind.None;
                }
                else
                {
                    store = new DirectoryStreamStore( path );
                    kind = CompressionKind.GZiped;
                }
                var a = new RuntimeArchive( m, store, kind );
                return a.IsValid ? a : null;
            }
            catch( Exception ex )
            {
                m.Fatal( $"While opening or creating zip file '{path}'.", ex );
                return null;
            }
        }

    }
}
