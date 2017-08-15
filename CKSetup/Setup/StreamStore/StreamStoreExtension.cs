using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.StreamStore
{
    static public class StreamStoreExtension
    {
        /// <summary>
        /// Creates a new entry with an initial text content.
        /// </summary>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <param name="text">Text (can not be null).</param>
        /// <param name="storageKind">Specifies the content's stream storage compression.</param>
        static public void CreateText( this IStreamStore @this, string fullName, string text, CompressionKind storageKind )
        {
            Action<Stream> writer = w =>
                                    {
                                        byte[] b = Encoding.UTF8.GetBytes( text );
                                        w.Write( b, 0, b.Length );
                                    };
            @this.Create( fullName,
                          storageKind == CompressionKind.GZiped ? GetCompressShell( writer ) : writer,
                          storageKind );
        }

        /// <summary>
        /// Updates an entry, optionnaly allow creating it if it does not exists.
        /// </summary>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <param name="text">Text (can not be null).</param>
        /// <param name="storageKind">Specifies the content's stream storage compression.</param>
        /// <param name="allowCreate">True to automatically creates the entry if it does not already exist.</param>
        static public void UpdateText( this IStreamStore @this, string fullName, string text, CompressionKind storageKind, bool allowCreate = false )
        {
            Action<Stream> writer = w =>
            {
                byte[] b = Encoding.UTF8.GetBytes( text );
                w.Write( b, 0, b.Length );
            };
            @this.Update( fullName,
                          storageKind == CompressionKind.GZiped ? GetCompressShell( writer ) : writer,
                          storageKind,
                          allowCreate );
        }

        /// <summary>
        /// Reads an existing resource previously written by <see cref="CreateText"/>
        /// or null if it does not exist.
        /// </summary>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <returns>The string or null if not found.</returns>
        static public string ReadText( this IStreamStore @this, string fullName )
        {
            var s = OpenUncompressedRead( @this, fullName );
            if( s == null ) return null;
            using( var r = new StreamReader( s, Encoding.UTF8, false ) )
            {
                return r.ReadToEnd();
            }
        }

        /// <summary>
        /// Tries to open an existing resource stream, uncompressing the data as necessary.
        /// Null if it does not exist.
        /// </summary>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <returns>The uncompressed data stream or null.</returns>
        static public Stream OpenUncompressedRead( this IStreamStore @this, string fullName )
        {
            return OpenRead( @this, fullName, CompressionKind.None ).Stream;
        }

        /// <summary>
        /// Tries to open an existing resource stream, uncompressing the data as necessary
        /// or letting it be compressed if the storage compression is the <paramref name="preferred"/> one.
        /// Null if it does not exist.
        /// </summary>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <returns>An opened readable stream along with its compression kind (or a null <see cref="StoredStream.Stream"/> if it does not exist).</returns>
        static public StoredStream OpenRead( this IStreamStore @this, string fullName, CompressionKind preferred )
        {
            StoredStream s = @this.OpenRead( fullName );
            if( s.Stream == null ) return s;
            switch( s.Kind )
            {
                case CompressionKind.None:
                    return s;
                case CompressionKind.GZiped:
                    switch( preferred )
                    {
                        case CompressionKind.None: return new StoredStream( preferred, new GZipStream( s.Stream, CompressionMode.Decompress, leaveOpen: true ) );
                        case CompressionKind.GZiped: return s;
                    }
                    break;
            }
            throw new ArgumentException( $"Unknown {s.Kind} or {preferred}.", nameof( s.Kind ) );
        }


        /// <summary>
        /// Creates a new entry with an initial content from an input stream.
        /// </summary>
        /// <param name="this">This component storage.</param>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <param name="input"></param>
        /// <param name="inputKind">Specifies the content's stream compression.</param>
        /// <param name="storageKind">Specifies the content's stream storage compression.</param>
        static public void Create( this IStreamStore @this, string fullName, Stream input, CompressionKind inputKind, CompressionKind storageKind )
        {
            if( input == null ) throw new ArgumentNullException( nameof( input ) );
            switch( inputKind )
            {
                case CompressionKind.None:
                    switch( storageKind )
                    {
                        case CompressionKind.None:
                            @this.Create( fullName, w => input.CopyTo( w ), storageKind );
                            return;
                        case CompressionKind.GZiped:
                            @this.Create( fullName, GetCompressShell( w => input.CopyTo( w ) ), storageKind );
                            return;
                    }
                    break;
                case CompressionKind.GZiped:
                    switch( storageKind )
                    {
                        case CompressionKind.None:
                            using( var decompressor = new GZipStream( input, CompressionMode.Decompress, true ) )
                            {
                                @this.Create( fullName, w => decompressor.CopyTo( w ), storageKind );
                            }
                            return;
                        case CompressionKind.GZiped:
                            @this.Create( fullName, w => input.CopyTo( w ), storageKind );
                            return;
                    }
                    break;
            }
            throw new ArgumentException( $"Unknown {inputKind} or {storageKind}.", "kind" );
        }

        /// <summary>
        /// Updates an entry, optionnaly allow creating it if it does not exists.
        /// </summary>
        /// <param name="this">This component storage.</param>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <param name="writer">The stream writer.</param>
        /// <param name="inputKind">Specifies the content's writer stream compression.</param>
        /// <param name="storageKind">Specifies the content's stream storage compression.</param>
        /// <param name="allowCreate">True to automatically creates the entry if it does not already exist.</param>
        static public void Update( this IStreamStore @this, string fullName, Action<Stream> writer, CompressionKind inputKind, CompressionKind storageKind, bool allowCreate )
        {
            if( writer == null ) throw new ArgumentNullException( nameof( writer ) );
            switch( inputKind )
            {
                case CompressionKind.None:
                    switch( storageKind )
                    {
                        case CompressionKind.None:
                            @this.Update( fullName, writer, storageKind, allowCreate );
                            return;
                        case CompressionKind.GZiped:
                            @this.Update( fullName, GetCompressShell( writer ), storageKind, allowCreate );
                            return;
                    }
                    break;
                case CompressionKind.GZiped:
                    switch( storageKind )
                    {
                        case CompressionKind.None:
                            using( var buffer = new MemoryStream() )
                            {
                                writer( buffer );
                                buffer.Position = 0;
                                using( var decompressor = new GZipStream( buffer, CompressionMode.Decompress, true ) )
                                {
                                    @this.Update( fullName, w => decompressor.CopyTo( w ), storageKind, allowCreate );
                                }
                            }
                            return;
                        case CompressionKind.GZiped:
                            @this.Update( fullName, writer, storageKind, allowCreate );
                            return;
                    }
                    break;
            }
            throw new ArgumentException( $"Unknown {inputKind} or {storageKind}.", "kind" );
        }

        /// <summary>
        /// Creates a stream compressor wrapper action.
        /// </summary>
        /// <param name="writer">Stream writer.</param>
        /// <returns>The writer or an adapted writer.</returns>
        static public Action<Stream> GetCompressShell( Action<Stream> writer )
        {
            return w =>
            {
                using( var compressor = new GZipStream( w, CompressionLevel.Optimal, true ) )
                {
                    writer( compressor );
                    compressor.Flush();
                }
            };
        }

        /// <summary>
        /// Creates a stream compressor wrapper action.
        /// </summary>
        /// <param name="writer">Stream writer.</param>
        /// <returns>The writer or an adapted writer.</returns>
        static public Func<Stream,Task> GetCompressShellAsync( Func<Stream,Task> writer )
        {
            return async w =>
            {
                using( var compressor = new GZipStream( w, CompressionLevel.Optimal, true ) )
                {
                    await writer( compressor );
                    compressor.Flush();
                }
            };
        }

    }
}
