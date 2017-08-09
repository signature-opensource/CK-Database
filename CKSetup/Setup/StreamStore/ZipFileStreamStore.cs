using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.StreamStore
{
    public class ZipFileStreamStore : IStreamStore
    {
        ZipArchive _archive;
        readonly string _path;

        struct MetaEntry
        {
            public readonly CompressionKind Kind;
            public readonly ZipArchiveEntry Entry;

            public MetaEntry( ZipArchiveEntry e, CompressionKind k )
            {
                Kind = k;
                Entry = e;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="ZipFileStreamStore"/> on a zip:
        /// uses <see cref="ZipFile.Open(string,ZipArchiveMode)"/> with <see cref="ZipArchiveMode.Update"/>
        /// that opens an existing file or creates a new archive if it does not exists.
        /// </summary>
        /// <param name="path">The local path to the zip file.</param>
        public ZipFileStreamStore( string path )
        {
            _path = Path.GetFullPath( path );
            _archive = ZipFile.Open( _path, ZipArchiveMode.Update );
        }

        MetaEntry Find( string fullName )
        {
            Debug.Assert( Enum.GetNames( typeof( CompressionKind ) ).SequenceEqual( new[] { "None", "GZiped" } ) );
            fullName = fullName.ToLowerInvariant();
            ZipArchiveEntry e = _archive.GetEntry( "None/" + fullName );
            if( e != null ) return new MetaEntry( e, CompressionKind.None );
            e = _archive.GetEntry( "GZiped/" + fullName );
            if( e != null ) return new MetaEntry( e, CompressionKind.GZiped );
            return new MetaEntry();
        }

        bool IStreamStore.IsEmptyStore => _archive.Entries.Count == 0;

        bool IStreamStore.Exists( string fullName ) => Find( fullName ).Entry != null;

        void IStreamStore.Create( string fullName, Action<Stream> writer, CompressionKind storageKind )
        {
            if( Find( fullName ).Entry != null ) throw new ArgumentException( $"{fullName} already exists.", nameof( fullName ) );
            fullName = storageKind.ToString() + '/' + fullName.ToLowerInvariant();
            var e = _archive.CreateEntry( fullName );
            try
            {
                using( var output = e.Open() )
                {
                    writer( output );
                }
            }
            catch( Exception )
            {
                e.Delete();
                throw;
            }
        }

        void IStreamStore.Update( string fullName, Action<Stream> writer, CompressionKind storageKind, bool allowCreate )
        {
            var e = Find( fullName );
            var zE = e.Entry;
            if( zE == null && !allowCreate ) throw new ArgumentException( $"{fullName} does not exist.", nameof( fullName ) );
            if( e.Kind != storageKind )
            {
                if( zE != null ) zE.Delete();
            }
            else if( zE != null )
            {
                // Did not find a way to actually reset the stream for updates:
                // Shorter updated stream (via entry.Open()) lets the internal MemoryStream
                // with the length of the previous stream.
                zE.Delete();
                _archive.Dispose();
                _archive = ZipFile.Open( _path, ZipArchiveMode.Update );
            }
            fullName = storageKind.ToString() + '/' + fullName.ToLowerInvariant();
            zE = _archive.CreateEntry( fullName );
            using( var output = zE.Open() )
            {
                writer( output );
            }
        }

        void IStreamStore.Delete( string fullName )
        {
            var e = Find( fullName );
            if( e.Entry != null ) e.Entry.Delete();
        }

        void IDisposable.Dispose()
        {
            if( _archive != null )
            {
                _archive.Dispose();
                _archive = null;
            }
        }

        void IStreamStore.Flush()
        {
            _archive.Dispose();
            _archive = ZipFile.Open( _path, ZipArchiveMode.Update );
        }

        StoredStream IStreamStore.OpenRead( string fullName )
        {
            var e = Find( fullName );
            if( e.Entry == null ) return new StoredStream();
            return new StoredStream( e.Kind, e.Entry.Open() );
        }

        void IStreamStore.ExtractToFile( string fullName, string targetPath )
        {
            var e = Find( fullName );
            if( e.Entry == null ) throw new ArgumentException( $"'{fullName}' not found.", nameof(fullName) );
            e.Entry.ExtractToFile( targetPath, false );
        }

        int IStreamStore.Delete( Func<string, bool> predicate )
        {
            int count = 0;
            foreach( var e in _archive.Entries )
            {
                if( predicate( RemoveCompressionPrefix( e.FullName ) ) )
                {
                    e.Delete();
                    ++count;
                }
            }
            return count;
        }

        static string RemoveCompressionPrefix( string name )
        {
            if( name.StartsWith( "None/" ) ) return name.Substring( 5 );
            Debug.Assert( name.StartsWith( "GZiped/" ) );
            return name.Substring( 7 );
        }
    }
}
