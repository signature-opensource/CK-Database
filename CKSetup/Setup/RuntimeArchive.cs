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
            bool success = true;
            if( _store.IsEmptyStore )
            {
                monitor.Info().Send( $"Creating new store." );
                _dbOrigin = _dbCurrent = new ComponentDB();
                _store.CreateText( DbXmlFileName, _dbCurrent.ToXml().ToString( SaveOptions.DisableFormatting ), CompressionKind.None );
            }
            else
            {
                try
                {
                    string text = _store.ReadText( DbXmlFileName );
                    if( text != null )
                    {
                        _dbOrigin = _dbCurrent = new ComponentDB( XDocument.Parse( text ).Root );
                        monitor.Trace().Send( $"Opened store: {_dbOrigin.Components.Count} components." );
                    }
                    else
                    {
                        monitor.Error().Send( $"File is not a valid runtime zip ({DbXmlFileName} manifest not found)." );
                        success = false;
                    }
                }
                catch( Exception ex )
                {
                    monitor.Fatal().Send( ex, $"Invalid {DbXmlFileName} manifest." );
                    success = false;
                }
            }
            if( !success )
            {
                _store.Dispose();
                _dbCurrent = null;
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
            using( _monitor.OpenInfo().Send( $"Clearing runtime archive." ) )
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
                    _monitor.Error().Send( ex );
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
            /// <param name="folders">Folders to add.</param>
            /// <returns>This importer (enable fluent syntax).</returns>
            public LocalImporter AddComponent( IEnumerable<BinFolder> folders )
            {
                if( folders == null ) throw new ArgumentNullException( nameof( folders ) );
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
            using( _monitor.OpenInfo().Send( $"Importing local folders." ) )
            {
                ComponentDB db = _dbCurrent;
                var added = new Dictionary<ComponentRef, BinFolder>();
                foreach( var f in folders )
                {
                    using( _monitor.OpenInfo().Send( $"Adding {f.BinPath}." ) )
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
                        using( _monitor.OpenInfo().Send( $"Components have missing embedded components. Trying to use importer." ) )
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
                        using( _monitor.OpenError().Send( $"Components have missing embedded components. Embedded components are required to be resolved." ) )
                        {
                            foreach( var c in newC.Where( c => c.C.Embedded.Count > 0 ) )
                            {
                                _monitor.Trace().Send( $"'{c.C}' embedds: '{c.C.Embedded.Select( e => e.ToString() ).Concatenate( "', '" )}'." );
                            }
                        }
                        return false;
                    }
                }
                foreach( var c in newC )
                {
                    foreach( var f in c.C.Files )
                    {
                        using( _monitor.OpenTrace().Send( $"Importing file {f.Name} content." ) )
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
                                _monitor.Error().Send( ex );
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
                using( monitor.OpenInfo().Send( "Starting import." ) )
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
        /// <param name="runPath">Optional run path: the first target <see cref="BinFolder.BinPath"/> is the default.</param>
        /// <returns>True on success, false on error.</returns>
        public bool ExtractRuntimeDependencies( IEnumerable<BinFolder> targets, string runPath = null, IComponentImporter missingImporter = null )
        {
            if( !targets.Any() ) throw new ArgumentException( "At least one target is required.", nameof( targets ) );
            using( _monitor.OpenInfo().Send( $"Extracting runtime support for '{targets.Select( t => t.BinPath ).Concatenate()}'." ) )
            {
                if( runPath == null ) runPath = targets.First().BinPath;
                _monitor.Info().Send( $"Extracting to {runPath}." );
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
                    using( _monitor.OpenInfo().Send( $"{c.Files.Count} files from '{c}'." ) )
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
                                            _monitor.Error().Send( $"Missing file '{f}' in local store." );
                                            return false;
                                        }
                                        if( !_store.Download( _monitor, missingImporter, f, _storageKind ) ) return false;
                                    }
                                    _store.ExtractToFile( fileKey, targetPath );
                                    _monitor.Debug().Send( $"Extracted {f.Name}." );
                                    _cleanupFiles.Add( targetPath );
                                    ++count;
                                }
                                catch( Exception ex )
                                {
                                    _monitor.Error().Send( ex, $"While extracting '{targetPath}'." );
                                    return false;
                                }
                            }
                            else _monitor.Trace().Send( $"Skipped '{targetPath}' since it already exists." );
                        }
                    }
                    _monitor.Info().Send( $"{count} files extracted." );
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
        public bool ImportComponents( Stream input, IComponentFileDownloader downloader = null )
        {
            using( _monitor.OpenInfo().Send( "Starting import." ) )
            {
                var n = _dbCurrent.Import( _monitor, input );
                if( n.Error ) return false;
                if( downloader != null )
                {
                    var r = _store.DownloadImportResult( _monitor, downloader, n, _storageKind );
                    if( r.Item2 > 0 )
                    {
                        _monitor.Error().Send( $"{r.Item2} download errors. Import canceled." );
                        return false;
                    }
                }
                _dbCurrent = n.NewDB;
                SaveDbCurrent();
                return true;
            }
        }

        void SaveDbCurrent()
        {
            if( _dbOrigin != _dbCurrent )
            {
                _store.UpdateText( DbXmlFileName, _dbCurrent.ToXml().ToString(), CompressionKind.None );
                _store.Flush();
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
            using( _monitor.OpenInfo().Send( "Closing Zip archive." ) )
            {
                if( _cleanupFiles.Count > 0 )
                {
                    using( _monitor.OpenTrace().Send( $"Cleaning {_cleanupFiles.Count} runtime files." ) )
                    {
                        foreach( var f in _cleanupFiles )
                        {
                            try
                            {
                                File.Delete( f );
                            }
                            catch( Exception ex )
                            {
                                _monitor.Warn().Send( ex );
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
                m.Fatal().Send( ex, $"While opening or creating zip file '{path}'." );
                return null;
            }
        }

    }
}
