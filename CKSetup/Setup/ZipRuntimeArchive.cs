using CK.Core;
using CK.Text;
using CSemVer;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup
{
    public class ZipRuntimeArchive : IDisposable
    {
        readonly ZipArchive _archive;
        readonly List<string> _cleanupFiles;
        readonly IActivityMonitor _monitor;
        readonly ComponentDB _dbOrigin;
        readonly ZipArchiveEntry _dbEntry;
        readonly EventSink _sink;
        readonly IComponentDBRemote _remote;
        ComponentDB _dbCurrent;

        class EventSink : IComponentDBEventSink
        {
            readonly Queue<Tuple<ComponentRef, object>> _events = new Queue<Tuple<ComponentRef, object>>();

            public void ComponentLocallyAdded( Component c, BinFolder f )
            {
                if( c.ComponentKind != ComponentKind.Model )
                {
                    Events.Enqueue( Tuple.Create( c.GetRef(), (object)f ) );
                }
            }

            public void ComponentRemoved( Component c )
            {
                if( c.ComponentKind != ComponentKind.Model )
                {
                    Events.Enqueue( Tuple.Create( c.GetRef(), (object)null ) );
                }
            }

            public void FilesRemoved( Component c, IReadOnlyList<string> files )
            {
                if( c.ComponentKind != ComponentKind.Model )
                {
                    Events.Enqueue( Tuple.Create( c.GetRef(), (object)files ) );
                }
            }

            public int GetSavePoint() => Events.Count;

            public void Cancel( int savedPoint )
            {
                while( Events.Count > savedPoint ) Events.Dequeue();
            }

            public Queue<Tuple<ComponentRef, object>> Events => _events;
        }

        ZipRuntimeArchive( IActivityMonitor monitor, string path, IComponentDBRemote remote = null )
        {
            _sink = new EventSink();
            _archive = ZipFile.Open( path, ZipArchiveMode.Update );
            _cleanupFiles = new List<string>();
            _remote = remote;
            _monitor = monitor;
            if( _archive.Entries.Count == 0 )
            {
                monitor.Info().Send( $"Creating new zip file." );
                _dbCurrent = new ComponentDB( _sink );
                _dbEntry = _archive.CreateEntry( "db.xml" );
            }
            else
            {
                _dbEntry = _archive.GetEntry( "db.xml" );
                if( _dbEntry != null )
                {
                    try
                    {
                        using( var content = _dbEntry.Open() )
                        {
                            _dbOrigin = _dbCurrent = new ComponentDB( _sink, XDocument.Load( content ).Root );
                            monitor.Trace().Send( $"Opened zip: {_dbOrigin.Components.Count} components, {_archive.Entries.Count} files." );
                        }
                    }
                    catch( Exception ex )
                    {
                        _dbEntry = null;
                        monitor.Fatal().Send( ex, "Invalid db.xml manifest." );
                    }
                }
                else
                {
                    monitor.Error().Send( "File is not a valid runtime zip (db.xml manifest not found)." );
                }
            }
            if( _dbEntry == null ) _archive.Dispose();
        }

        /// <summary>
        /// Gets whether this database is has been successfully opened.
        /// </summary>
        public bool IsValid => _dbEntry != null;

        /// <summary>
        /// Registers a file that will be automatically deleted when this <see cref="ZipRuntimeArchive"/>
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
                                    .Where( d => !_dbCurrent.Components.Any( c => c.Name == d.UseName && (d.UseMinVersion == null || c.Version == d.UseMinVersion) ))
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
            using( _monitor.OpenInfo().Send( $"Removing entries in runtime zip." ) )
            {
                try
                {
                    int count = 0;
                    foreach( var e in _archive.Entries )
                    {
                        if( e == _dbEntry ) continue;
                        e.Delete();
                        ++count;
                    }
                    _dbCurrent = new ComponentDB( _sink );
                    _monitor.CloseGroup( $"{count} entries removed." );
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
        /// Adds a component. Ignores it if it already registered.
        /// </summary>
        /// <param name="folder">The component folder.</param>
        /// <returns>True on success, false on failure.</returns>
        public bool AddComponent( BinFolder folder )
        {
            if( folder == null ) throw new ArgumentNullException( nameof( folder ) );
            if( !IsValid ) throw new InvalidOperationException();
            using( _monitor.OpenInfo().Send( $"Adding components from '{folder.BinPath}'." ) )
            {
                var n = _dbCurrent.Add( _monitor, folder );
                if( n == null ) return false;
                _dbCurrent = n;
            }
            return true;
        }

        /// <summary>
        /// Extracts required runtime support for Models in a targets.
        /// </summary>
        /// <param name="targets">The target folders.</param>
        /// <param name="runPath">Optional run path: the first target <see cref="BinFolder.BinPath"/> is the default.</param>
        /// <returns>True on success, false on error.</returns>
        public bool ExtractRuntimeDependencies( IEnumerable<BinFolder> targets, string runPath = null )
        {
            if( !targets.Any() ) throw new ArgumentException( "At least one target is required.", nameof( targets ) );
            using( _monitor.OpenInfo().Send( $"Extracting runtime support for '{targets.Select( t => t.BinPath ).Concatenate()}'." ) )
            {
                if( runPath == null ) runPath = targets.First().BinPath;
                _monitor.Info().Send( $"Extracting to {runPath}." );
                var resolver = _dbCurrent.GetRuntimeDependenciesResolver( _monitor, targets );
                if( resolver == null ) return false;
                var savedPoint = _sink.GetSavePoint();
                var r = resolver.Run( _monitor, _remote );
                if( r.Key == null )
                {
                    _sink.Cancel( savedPoint );
                    return false;
                }
                if( HasChanges ) CommitChanges();
                _dbCurrent = r.Key;
                var components = r.Value;
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
                                    string entry = c.GetRef().EntryPathPrefix + f;
                                    var e = _archive.GetEntry( entry );
                                    if( e == null )
                                    {
                                        throw new InvalidOperationException( $"'{entry}' file not found: CommitChanges must be called firt." );
                                    }
                                    e.ExtractToFile( targetPath );
                                    _monitor.Debug().Send( $"Extracted {e.Name}." );
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
        /// Gets whether changes are pending.
        /// Note that <see cref="CommitChanges"/> is automatically called by <see cref="Dispose"/>.
        /// </summary>
        public bool HasChanges => _sink.Events.Count > 0;

        /// <summary>
        /// Commit current changes to this archive.
        /// </summary>
        public void CommitChanges()
        {
            if( !HasChanges ) return;
            using( _monitor.OpenInfo().Send( $"Committing {_sink.Events.Count} changes." ) )
            {
                while( _sink.Events.Count > 0 )
                {
                    var e = _sink.Events.Dequeue();
                    if( e.Item2 == null )
                    {
                        int nbDeleted = 0;
                        foreach( var toDel in _archive.Entries.Where( x => x.FullName.StartsWith( e.Item1.EntryPathPrefix ) ) )
                        {
                            _monitor.Debug().Send( "Deleting: " + toDel.FullName );
                            toDel.Delete();
                            ++nbDeleted;
                        }
                        _monitor.Info().Send( $"Component '{e.Item1}' has been removed ({nbDeleted} files removed)." );
                    }
                    else
                    {
                        var filesToRemove = e.Item2 as IReadOnlyList<string>;
                        if( filesToRemove != null )
                        {
                            int nbDeleted = 0;
                            var rem = new HashSet<string>( filesToRemove.Select( f => e.Item1.EntryPathPrefix + f ) );
                            foreach( var toDel in _archive.Entries.Where( x => rem.Contains( x.FullName ) ) )
                            {
                                _monitor.Debug().Send( "Deleting: " + toDel.FullName );
                                toDel.Delete();
                                ++nbDeleted;
                            }
                            _monitor.Info().Send( $"{nbDeleted} files removed for '{e.Item1}'." );
                        }
                        else
                        {
                            var newC = _dbCurrent.Find( e.Item1 );
                            var zipPathPrefix = newC.GetRef().EntryPathPrefix;
                            var folder = (BinFolder)e.Item2;
                            foreach( var f in newC.Files )
                            {
                                string source = folder.BinPath + f;
                                string entryPath = zipPathPrefix + f;
                                _monitor.Debug().Send( "Archiving: " + entryPath );
                                _archive.CreateEntryFromFile( source, entryPath );
                            }
                            _monitor.Info().Send( $"Created '{e.Item1}' (registered {newC.Files.Count} files)." );
                        }
                    }
                }
            }
        }

        static public ZipRuntimeArchive OpenOrCreate( IActivityMonitor m, string path, IComponentDBRemote remote = null )
        {
            try
            {
                var z = new ZipRuntimeArchive( m, path, remote );
                return z.IsValid ? z : null;
            }
            catch( Exception ex )
            {
                m.Fatal().Send( ex, $"While opening or creating zip file '{path}'." );
                return null;
            }
        }

        public void Dispose()
        {
            if( _dbEntry == null ) return;
            using( _monitor.OpenInfo().Send( "Closing Zip archive." ) )
            {
                CommitChanges();
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
                if( _dbCurrent != _dbOrigin )
                {
                    using( var content = _dbEntry.Open() )
                    {
                        new XDocument( _dbCurrent.ToXml() ).Save( content );
                    }
                    _dbCurrent = _dbOrigin;
                }
                _archive.Dispose();
            }
        }
    }
}
