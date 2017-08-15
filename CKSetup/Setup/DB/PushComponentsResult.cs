using CK.Core;
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
        /// <param name="files">
        /// Missing files identifiers. Must not be null but can be empty 
        /// if all files are already stored.
        /// </param>
        /// <param name="sessionId">
        /// Session identifier. Can be null or whitespace only if files is empty.
        /// </param>
        public PushComponentsResult( IReadOnlyCollection<SHA1Value> files, string sessionId )
        {
            if( files == null ) throw new ArgumentNullException( nameof( files ) );
            if( files.Count > 0 && string.IsNullOrWhiteSpace( sessionId ) ) throw new ArgumentNullException( nameof( sessionId ) );
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

        public PushComponentsResult( CKBinaryReader r )
        {
            int version = r.ReadNonNegativeSmallInt32();
            SessionId = r.ReadNullableString();
            ErrorText = r.ReadNullableString();
            if( ErrorText == null )
            {
                var all = new SHA1Value[r.ReadNonNegativeSmallInt32()];
                for( int i = 0; i < all.Length; ++i ) all[i] = new SHA1Value( r );
                Files = all;
            }
        }

        public void Write( CKBinaryWriter w )
        {
            w.WriteNonNegativeSmallInt32( 0 );
            w.WriteNullableString( SessionId );
            w.WriteNullableString( ErrorText );
            if( ErrorText == null )
            {
                w.WriteNonNegativeSmallInt32( Files.Count );
                foreach( var f in Files ) f.Write( w );
            }
        }

        /// <summary>
        /// Gets the session identifier that identifies this push
        /// on the server side.
        /// This is not required to be set if <see cref="ErrorText"/> is not null.
        /// </summary>
        public string SessionId { get; }
        
        /// <summary>
        /// Gets the files identifiers that are required.
        /// Null when <see cref="ErrorText"/> is not null.
        /// </summary>
        public IReadOnlyCollection<SHA1Value> Files { get; }

        /// <summary>
        /// Gets an error description if an error occurs. Null otherwise.
        /// </summary>
        public string ErrorText { get; }

    }
}
