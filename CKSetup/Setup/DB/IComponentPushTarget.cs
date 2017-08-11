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
    /// Defines the required behavior of a target store. 
    /// </summary>
    public interface IComponentPushTarget
    {
        /// <summary>
        /// Pushes a stream of components description and returns
        /// a collection of required files.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="componentsWriter">Component streamer.</param>
        /// <returns>A result object.</returns>
        PushComponentsResult PushComponents( IActivityMonitor monitor, Action<Stream> componentsWriter );

        /// <summary>
        /// Pushes a file.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="sessionId">The target's session identifier (<see cref="PushComponentsResult.SessionId"/>).</param>
        /// <param name="sha1">The file SHA1.</param>
        /// <param name="writer">Stream writer.</param>
        /// <param name="kind">Compression kind of the stream.</param>
        /// <returns></returns>
        bool PushFile( IActivityMonitor monitor, string sessionId, SHA1Value sha1, Action<Stream> writer, CompressionKind kind );
    }
}
