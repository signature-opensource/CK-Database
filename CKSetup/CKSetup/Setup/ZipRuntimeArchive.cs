using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    public class ZipRuntimeArchive : IDisposable
    {
        readonly ZipArchive _archive;
        readonly List<string> _cleanupFiles;
        readonly IActivityMonitor _monitor;

        ZipRuntimeArchive( IActivityMonitor monitor, string path )
        {
            _archive = ZipFile.Open( path, ZipArchiveMode.Update );
            _cleanupFiles = new List<string>();
            _monitor = monitor;
        }

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
        public bool AddOrUpdateEngine( BinFileInfo f )
        {
            using( _monitor.OpenInfo().Send( $"Adding engine '{f.Name}'." ) )
            {
                //if( !AddOrUpdateAssembly( f ) ) return false;
                //foreach( var dep in f.LocalDependencies )
                //{
                //    if( !AddOrUpdateAssembly( dep ) ) return false;
                //}
                //if( !AddOrUpdateRuntimeDependenciesInfo( f ) ) return false;
            }
            return true;
        }

        bool AddOrUpdateAssembly( BinFileInfo f )
        {
            try
            {
                //string entryName = f.ZipEntryName;
                //var e = _archive.GetEntry( entryName ) ?? _archive.CreateEntry( entryName, CompressionLevel.Optimal );
                //using( var source = new FileStream( f.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan ) )
                //using( var content = e.Open() )
                //{
                //    source.CopyTo( content );
                //}
                return true;
            }
            catch( Exception ex )
            {
                _monitor.Error().Send( ex, $"While adding '{f.FullPath}'." );
                return false;
            }
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

        bool AddOrUpdateRuntimeDependenciesInfo( BinFileInfo f )
        {
            try
            {
                //string entryName = f.ZipEntryPath + "\\deps.txt";
                //var e = _archive.GetEntry( entryName ) ?? _archive.CreateEntry( entryName, CompressionLevel.Optimal );
                //using( var content = e.Open() )
                //using( var w = new StreamWriter( content ) )
                //{
                //    w.WriteLine( f.ZipEntryName );
                //    foreach( var dep in f.LocalDependencies )
                //    {
                //        w.WriteLine( dep.ZipEntryName );
                //    }
                //    w.Flush();
                //}
                return true;
            }
            catch( Exception ex )
            {
                _monitor.Error().Send( ex, $"While adding dependencies of '{f.FullPath}'." );
                return false;
            }
        }

        static public ZipRuntimeArchive OpenOrCreate( IActivityMonitor m, string path )
        {
            try
            {
                return new ZipRuntimeArchive( m, path );
            }
            catch( Exception ex )
            {
                m.Fatal().Send( ex, $"While opening or creating zip file '{path}'." );
                return null;
            }
        }

        public void Dispose()
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
        }
    }
}
