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
        /// Gets a simple projection of the missing dependencies without 
        /// considering target framework and simplified to the highest required version.
        /// This is not really representative of what is actually missing and is exposed here
        /// mainly for tests purpose.
        /// </summary>
        public IEnumerable<ComponentDependency> SimpleMissingRegistrations
        {
            get
            {
                return _dbCurrent.Components.SelectMany( c => c.Dependencies )
                                    .Where( d => !_dbCurrent.Components.Any( c => c.Name == d.UseName && (d.UseMinVersion == null || c.Version == d.UseMinVersion) ) )
                                .Concat( _dbCurrent.EmbeddedComponents.Select( e => new ComponentDependency( e.Name, e.Version ) ) )
                                .GroupBy( d => d.UseName )
                                .Select( g => g.MaxBy( d => d.UseMinVersion ) );
            }
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

        /// <summary>
        /// Adds a component. Ignores it when already registered.
        /// </summary>
        /// <param name="folder">The component folder.</param>
        /// <returns>True on success, false on failure.</returns>
        public bool AddComponent( BinFolder folder )
        {
            if( folder == null ) throw new ArgumentNullException( nameof( folder ) );
            if( !IsValid ) throw new InvalidOperationException();
            using( _monitor.OpenInfo().Send( $"Adding components from '{folder.BinPath}'." ) )
            {
                var result = _dbCurrent.Add( _monitor, folder );
                if( result.Error ) return false;
                if( result.NewComponent != null )
                {
                    var newFileKeys = new List<string>();
                    foreach( var f in folder.Files )
                    {
                        using( _monitor.OpenTrace().Send( $"Importing file {f.LocalFileName} content." ) )
                        {
                            string key = f.ContentSHA1.ToString();
                            if( _store.Exists( key ) )
                            {
                                _monitor.CloseGroup( "Already stored." );
                            }
                            else
                            {
                                using( var content = File.OpenRead( f.FullPath ) )
                                {
                                    _store.Create( key, content, CompressionKind.None, _storageKind );
                                }
                                newFileKeys.Add( key );
                            }
                        }
                    }
                }
                _dbCurrent = result.NewDB;
                SaveDbCurrent();
            }
            return true;
        }

        class ComponentDownloader : IComponentDownloader
        {
            readonly RuntimeArchive _archive;
            readonly IComponentImporter _importer;

            public ComponentDownloader( RuntimeArchive a, IComponentImporter importer )
            {
                _archive = a;
                _importer = importer;
            }

            public ComponentDB Download( IActivityMonitor monitor, ComponentMissingDescription missing )
            {
                using( var s = _importer.OpenImportStream( monitor, missing ) )
                {
                    if( s == null || !_archive.ImportComponents( monitor, s, _importer ) ) return null;
                    return _archive._dbCurrent;
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
                                                    ? new ComponentDownloader( this, missingImporter )
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
                            string targetPath = runPath + f;
                            if( !File.Exists( targetPath ) )
                            {
                                try
                                {
                                    _store.ExtractToFile( f.SHA1.ToString(), targetPath );
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
        /// Imports a set of components from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="input">Input stream.</param>
        /// <returns>True on success, false on error.</returns>
        public bool ImportComponents( IActivityMonitor monitor, Stream input, IComponentFileDownloader downloader = null )
        {
            var n = _dbCurrent.Import( monitor, input );
            if( n == null )
            {
                return false;
            }
            _dbCurrent = n;
            return true;
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
        /// Closes this archive. <see cref="CommitChanges"/> is called if necessary, 
        /// files registered by <see cref="RegisterFileToDelete"/> are deleted and 
        /// updated component database is written in the zip.
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
