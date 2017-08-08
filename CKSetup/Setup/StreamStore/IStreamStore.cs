using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.StreamStore
{

    /// <summary>
    /// Defines required behavior for component store. 
    /// </summary>
    public interface IStreamStore : IDisposable
    {
        /// <summary>
        /// Gets whether this store is empty.
        /// </summary>
        bool IsEmptyStore { get; }

        /// <summary>
        /// Gets whether an entry exists. 
        /// Recalls that names are case insensitive.
        /// </summary>
        /// <param name="fullName">The entry name.</param>
        /// <returns>True if the entry exists, false otherwise.</returns>
        bool Exists( string fullName );

        /// <summary>
        /// Tries to open a stream on an existing resource.
        /// <see cref="StoredStream.Stream"/> is null if the resource does not exist.
        /// </summary>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <returns>An opened readable stream along with its compression kind (or a null <see cref="StoredStream.Stream"/> if it does not exist).</returns>
        StoredStream OpenRead( string fullName );

        /// <summary>
        /// Creates a new entry with an initial content.
        /// </summary>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <param name="writeContent">Stream writer action.</param>
        /// <param name="storageKind">Specifies the content's stream storage compression.</param>
        void Create( string fullName, Action<Stream> writeContent, CompressionKind storageKind );

        /// <summary>
        /// Updates an entry, optionnaly allow creating it if it does not exists.
        /// </summary>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <param name="writeContent">Stream writer action.</param>
        /// <param name="storageKind">Specifies the content's stream storage compression.</param>
        /// <param name="allowCreate">True to automatically creates the entry if it does not alreadt exist.</param>
        void Update( string fullName, Action<Stream> writeContent, CompressionKind storageKind, bool allowCreate = false );

        /// <summary>
        /// Extracts a file to the file system.
        /// The <paramref name="fullName"/> must exist and the <paramref name="targetPath"/> must not.
        /// </summary>
        /// <param name="fullName">The resource full name (case insensitive).</param>
        /// <param name="targetPath">Path of the target file.</param>
        void ExtractToFile( string fullName, string targetPath );

        /// <summary>
        /// Deletes an entry. This is idempotent: no error if it does not exists.
        /// </summary>
        /// <param name="fullName">The full name of the resource to destroy (case insensitive).</param>
        void Delete( string fullName );

        /// <summary>
        /// Deletes all files whose fullname matches a predicate.
        /// Recalls that names are case insensitive.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The number of deleted entries.</returns>
        int Delete( Func<string, bool> predicate );

        /// <summary>
        /// Flushes any intermediate data.
        /// Dispose method MUST call Flush.
        /// </summary>
        void Flush();
    }
}
