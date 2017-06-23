using CK.Core;
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
        ComponentDB _dbCurrent;

        class EventSink : IComponentDBEventSink
        {
            readonly Queue<Tuple<ComponentRef, object>> _events = new Queue<Tuple<ComponentRef, object>>();

            public void ComponentAdded( Component c, BinFolder f )
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

            public Queue<Tuple<ComponentRef, object>> Events => _events;
        }

        ZipRuntimeArchive( IActivityMonitor monitor, string path )
        {
            _sink = new EventSink();
            _archive = ZipFile.Open( path, ZipArchiveMode.Update );
            _cleanupFiles = new List<string>();
            _monitor = monitor;
            if( _archive.Entries.Count == 0 )
            {
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

        public bool IsValid => _dbEntry != null;

        public bool Clear()
        {
            using( _monitor.OpenInfo().Send( $"Removing entries in runtime zip." ) )
            {
                try
                {
                    int count = 0;
                    //foreach( var e in _archive.Entries )
                    //{
                    //    e.Delete();
                    //    ++count;
                    //}
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
        /// Adds or updates an engine.
        /// </summary>
        /// <param name="f">The runtime file.</param>
        /// <returns>True on success, false on failure.</returns>
        public bool AddComponent( BinFolder f )
        {
            if( !IsValid ) throw new InvalidOperationException();
            using( _monitor.OpenInfo().Send( $"Adding components from '{f.BinPath}'." ) )
            {
                var n = _dbCurrent.Add( _monitor, f );
                if( n == null ) return false;
                _dbCurrent = n;
            }
            return true;
        }
        public bool ExtractRuntimeDependencies( BinFolder target )
        {
            using( _monitor.OpenInfo().Send( $"Extracting runtime support into '{target.BinPath}'." ) )
            {
                //var entryDedup = new Dictionary<string,ZipArchiveEntry>();
                //foreach( var dep in setupDependencies )
                //{
                //    string path = $@"{dep.Name}\{dep.Referencer.RawTargetFramework}\{dep.Version}";
                //    var e = _archive.GetEntry( $@"{path}\deps.txt" );
                //    if( e == null )
                //    {
                //        _monitor.Error().Send( $"Runtime dependency '{path}' for '{dep.Referencer.Name}' is not registered." );
                //        return false;
                //    }
                //    using( var content = e.Open() )
                //    using( var reader = new StreamReader( content ) )
                //    {
                //        string line;
                //        while( (line = reader.ReadLine()) != null )
                //        {
                //            if( !entryDedup.ContainsKey(line) )
                //            {
                //                var depEntry = _archive.GetEntry( line );
                //                if( depEntry == null )
                //                {
                //                    _monitor.Error().Send( $"Entry '{line}' for dependency of '{dep.Referencer.Name}' not found." );
                //                    return false;
                //                }
                //                entryDedup.Add( line, depEntry );
                //            }
                //        }
                //    }

                //}
                //foreach( var e in entryDedup.Values )
                //{
                //    if( !ExtractToBin( e, binPath ) ) return false;
                //}
            }
            return true;
        }

        //bool ExtractToBin( ZipArchiveEntry e, string binPath )
        //{
        //    string targetFile = Path.Combine( binPath, e.Name );
        //    if( !File.Exists( targetFile ) )
        //    {
        //        try
        //        {
        //            e.ExtractToFile( targetFile );
        //            _cleanupFiles.Add( targetFile );
        //            _monitor.Info().Send( $"Extracted {e.Name}." );
        //        }
        //        catch( Exception ex )
        //        {
        //            _monitor.Error().Send( ex, $"While extracting '{e.FullName}'." );
        //            return false;
        //        }
        //    }
        //    else _monitor.Info().Send( $"Skipped '{e.Name}' since it already exists." );
        //    return true;
        //}

        static public ZipRuntimeArchive OpenOrCreate( IActivityMonitor m, string path )
        {
            try
            {
                var z = new ZipRuntimeArchive( m, path );
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
            while( _sink.Events.Count > 0 )
            {
                var e = _sink.Events.Dequeue();
                if( e.Item2 == null )
                {
                    foreach( var toDel in _archive.Entries.Where( x => x.FullName.StartsWith( e.Item1.EntryPathPrefix ) ) )
                    {
                        toDel.Delete();
                    }
                }
                else
                {
                    var filesToRemove = e.Item2 as IReadOnlyList<string>;
                    if( filesToRemove != null )
                    {
                        var rem = new HashSet<string>( filesToRemove.Select( f => e.Item1.EntryPathPrefix + f ) );
                        foreach( var toDel in _archive.Entries.Where( x => rem.Contains( x.FullName ) ) )
                        {
                            toDel.Delete();
                        }
                    }
                    else
                    {
                        var newC = _dbCurrent.Find( e.Item1 );
                        var zipPathPrefix = newC.GetRef().EntryPathPrefix;
                        var folder = (BinFolder)e.Item2;
                        foreach( var f in newC.Files )
                        {
                            string source = folder.BinPath + f;
                            _archive.CreateEntryFromFile( source, zipPathPrefix + f );
                        }
                    }
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
