using CK.Core;
using CKSetup.StreamStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    /// <summary>
    /// Asynchronous file downloader.
    /// </summary>
    public interface IComponentFileDownloader
    {
        /// <summary>
        /// Gets a readable stream of a file content.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="file">SHA1 of the required file.</param>
        /// <param name="kind">The compression format of the returned stream.</param>
        /// <returns>An opened readable stream along with its compression kind (or a null <see cref="StoredStream.Stream"/> if it does not exist).</returns>
        Task<StoredStream> GetDownloadStreamAsync( IActivityMonitor monitor, SHA1Value file, CompressionKind kind );
    }
}
