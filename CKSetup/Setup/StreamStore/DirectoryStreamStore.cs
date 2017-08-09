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
    public class DirectoryStreamStore : IStreamStore
    {
        readonly string _path;

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
            _path = Path.GetFullPath( path );
            Directory.CreateDirectory( _path );
        }

        MetaEntry Find( string fullName )
        {
            Debug.Assert( Enum.GetNames( typeof( CompressionKind ) ).SequenceEqual( new[] { "None", "GZiped" } ) );
            fullName = fullName.ToLowerInvariant();
            FileInfo e = new FileInfo( Path.Combine( _path, "None", fullName ) );
            if( e.Exists ) return new MetaEntry( e, CompressionKind.None );
            e = new FileInfo( Path.Combine( _path, "GZiped", fullName ) );
            if( e != null ) return new MetaEntry( e, CompressionKind.GZiped );
            return new MetaEntry();
        }

        bool IStreamStore.IsEmptyStore => !Directory.EnumerateFileSystemEntries( _path ).Any();

        bool IStreamStore.Exists( string fullName ) => Find( fullName ).File != null;

        void IStreamStore.Create( string fullName, Action<Stream> writer, CompressionKind storageKind )
        {
            Debug.Assert( Enum.GetNames( typeof( CompressionKind ) ).SequenceEqual( new[] { "None", "GZiped" } ) );
            fullName = Path.Combine( _path, storageKind.ToString(), fullName.ToLowerInvariant() );
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

            fullName = Path.Combine( _path, storageKind.ToString(), fullName.ToLowerInvariant() );
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
            if( e.Kind == CompressionKind.None ) e.File.CopyTo( fullName, false );
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
            int count = DoDelete( predicate, Path.Combine( _path, "None" ) );
            count += DoDelete( predicate, Path.Combine( _path, "GZiped" ) );
            return count;
        }

        static int DoDelete( Func<string, bool> predicate,string prefix )
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
