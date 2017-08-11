using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{

    /// <summary>
    /// Captures the result of the initial <see cref="IComponentPushTarget.PushComponents"/> call.
    /// </summary>
    public class PushComponentsResult
    {
        /// <summary>
        /// Initializes a new successful result.
        /// </summary>
        /// <param name="files">Missing files identifiers.</param>
        /// <param name="sessionId">Session identifier. Can not be null nor whitespace.</param>
        public PushComponentsResult( IReadOnlyCollection<SHA1Value> files, string sessionId )
        {
            if( string.IsNullOrWhiteSpace( sessionId ) ) throw new ArgumentNullException( nameof( sessionId ) );
            if( files == null ) throw new ArgumentNullException( nameof( files ) );
            SessionId = sessionId;
            Files = files;
        }

        /// <summary>
        /// Initializes a new error result.
        /// </summary>
        /// <param name="error">The error message. Can not be null nor whitespace.</param>
        /// <param name="sessionId">Optional session identifier</param>
        public PushComponentsResult( string error, string sessionId )
        {
            if( string.IsNullOrWhiteSpace( error ) ) throw new ArgumentNullException( nameof( error ) );
            SessionId = sessionId;
            ErrorText = error;
        }

        /// <summary>
        /// Gets the session identifier that identifies this push
        /// on the server side.
        /// This is not required to be set if <see cref="ErrorText"/> is not null.
        /// </summary>
        public string SessionId { get; }
        
        /// <summary>
        /// Gets the files identifiers that are required.
        /// </summary>
        public IReadOnlyCollection<SHA1Value> Files { get; }

        /// <summary>
        /// Gets an error description if an error occurs. Null otherwise.
        /// </summary>
        public string ErrorText { get; }

    }
}
