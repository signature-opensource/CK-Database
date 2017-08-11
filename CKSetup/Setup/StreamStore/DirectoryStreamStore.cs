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
    public sealed class DirectoryStreamStore : IStreamStore
    {
        readonly string _path;
        readonly string _pathNone;
        readonly string _pathGZiped;
        readonly string[] _paths;

        struct MetaEntry
        {
            public readonly CompressionKind Kind;
            public readonly FileInfo File;

            public MetaEntry( FileInfo e, CompressionKind k )
            {
                Kind = k;
                File = e;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="DirectoryStreamStore"/> on a zip:
        /// the directory is created if it does not exist.
        /// An <see cref="IOException"/> is throw if the path is an existing file.
        /// </summary>
        /// <param name="path">The local directory path.</param>
        public DirectoryStreamStore( string path )
        {
            Debug.Assert( Enum.GetNames( typeof( CompressionKind ) ).SequenceEqual( new[] { "None", "GZiped" } ) );
            Debug.Assert( ((int[])Enum.GetValues( typeof( CompressionKind ) )).SequenceEqual( new[] { 0, 1 } ) );

            _path = Path.GetFullPath( path );
            _pathNone = FileUtil.NormalizePathSeparator( Path.Combine( _path, "None" ), true );
            _pathGZiped = FileUtil.NormalizePathSeparator( Path.Combine( _path, "GZiped" ), true );
            _paths = new string[] { _pathNone, _pathGZiped };

            Directory.CreateDirectory( _pathNone );
            Directory.CreateDirectory( _pathGZiped );
        }

        MetaEntry Find( string fullName )
        {
            fullName = fullName.ToLowerInvariant();
            FileInfo e = new FileInfo( _pathNone + fullName );
            if( e.Exists ) return new MetaEntry( e, CompressionKind.None );
            e = new FileInfo( _pathGZiped + fullName );
            if( e.Exists ) return new MetaEntry( e, CompressionKind.GZiped );
            return new MetaEntry();
        }

        /// <summary>
        /// Gets the full path of a file.
        /// </summary>
        /// <param name="k">The compression kind.</param>
        /// <param name="fullName">The entry name.</param>
        /// <returns>The full path of the stored file.</returns>
        public string GetFullPath( CompressionKind k, string fullName ) => _paths[(int)k] + fullName.ToLowerInvariant();

        bool IStreamStore.IsEmptyStore => !Directory.EnumerateFileSystemEntries( _pathNone ).Any()
                                          || !Directory.EnumerateFileSystemEntries( _pathGZiped ).Any();

        /// <summary>
        /// Checks whether teh entry exists (regardless of its actual <see cref="CompressionKind"/>).
        /// </summary>
        /// <param name="fullName">The entry name.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        public bool Exists( string fullName ) => Find( fullName ).File != null;

        void IStreamStore.Create( string fullName, Action<Stream> writer, CompressionKind storageKind )
        {
            fullName = GetFullPath( storageKind, fullName );
            try
            {
                using( var output = new FileStream( fullName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan ) )
                {
                    writer( output );
                }
            }
            catch( Exception )
            {
                File.Delete( fullName );
                throw;
            }
        }

        void IStreamStore.Update( string fullName, Action<Stream> writer, CompressionKind storageKind, bool allowCreate )
        {
            Debug.Assert( Enum.GetNames( typeof( CompressionKind ) ).SequenceEqual( new[] { "None", "GZiped" } ) );
            var e = Find( fullName );
            if( e.File == null && !allowCreate ) throw new ArgumentException( $"{fullName} does not exist.", nameof( fullName ) );
            if( e.File != null && e.Kind != storageKind ) e.File.Delete();

            fullName = GetFullPath( storageKind, fullName );
            using( var output = new FileStream( fullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan ) )
            {
                writer( output );
            }
        }

        void IStreamStore.Delete( string fullName )
        {
            var e = Find( fullName );
            if( e.File != null ) e.File.Delete();
        }

        void IDisposable.Dispose()
        {
        }

        void IStreamStore.Flush()
        {
        }

        StoredStream IStreamStore.OpenRead( string fullName )
        {
            var e = Find( fullName );
            if( e.File == null ) return new StoredStream();
            return new StoredStream( e.Kind, e.File.OpenRead() );
        }

        void IStreamStore.ExtractToFile( string fullName, string targetPath )
        {
            var e = Find( fullName );
            if( e.File == null ) throw new ArgumentException( $"'{fullName}' not found.", nameof( fullName ) );
            if( e.Kind == CompressionKind.None ) e.File.CopyTo( targetPath, false );
            else
            {
                using( var s = StreamStoreExtension.OpenUncompressedRead( this, fullName ) )
                using( var output = new FileStream( targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan ) )
                {
                    s.CopyTo( output );
                }
            }
        }

        int IStreamStore.Delete( Func<string, bool> predicate )
        {
            int count = DoDelete( predicate, _pathNone );
            count += DoDelete( predicate, _pathGZiped );
            return count;
        }

        static int DoDelete( Func<string, bool> predicate, string prefix )
        {
            int count = 0;
            foreach( var e in Directory.EnumerateFiles( prefix, "*", SearchOption.AllDirectories ) )
            {
                if( predicate( e.Substring( prefix.Length ) ) )
                {
                    File.Delete( e );
                    ++count;
                }
            }
            return count;
        }
    }
}
