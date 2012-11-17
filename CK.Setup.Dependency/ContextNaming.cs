using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public static class ContextNaming
    {

        /// <summary>
        /// Extracts a context prefix from a given string if it exists and gives the suffix: [context]fullName (or [context].fullName). 
        /// When no context prefix exists, context is set to <see cref="String.Empty"/> and fullName is the original unchanged string.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="context">Output context if found. Empty string if not found.</param>
        /// <param name="fullName">Suffix if a context prefix exists, the original <paramref name="s"/> otherwise.</param>
        /// <returns>True on success, false on error (context and fullName are null in this case).</returns>
        public static bool TryExtractContext( string s, out string context, out string fullName )
        {
            context = null;
            fullName = null;
            if( String.IsNullOrEmpty( s ) ) return false;
            if( s[0] == '[' )
            {
                int idxEnd = s.IndexOf( ']' );
                if( idxEnd < 0 || idxEnd == s.Length - 1 ) return false;
                context = s.Substring( 1, idxEnd - 1 );
                // We are kind enough to accept [Context].FullName dotted syntax.
                if( s[++idxEnd] == '.' ) ++idxEnd;
                fullName = s.Substring( idxEnd );
            }
            else
            {
                context = String.Empty;
                fullName = s;
            }
            return true;
        }

        /// <summary>
        /// Prefixes the string with a [context] if a context is not already specified (the fullName does not start with '[').
        /// </summary>
        /// <param name="fullName">String to check. When null or empty, [<paramref name="context"/>] is returned or <see cref="String.Empty"/> if context is also null or empty.</param>
        /// <param name="context">Context to inject. When null or empty, <paramref name="fullName"/> is returned as-is.</param>
        /// <returns>The string with the contex prefix. Never null: when both <paramref name="fullName"/> and <paramref name="context"/> are null or empty, the empty string is returned.</returns>
        public static string SetContextPrefixIfNotExists( string fullName, string context )
        {
            if( String.IsNullOrEmpty( fullName ) )
            {
                if( String.IsNullOrEmpty( context ) ) return String.Empty;
                return '[' + context + ']';
            }
            if( String.IsNullOrEmpty( context ) || fullName[0] == '[' ) return fullName;
            return String.Format( "[{0}]{1}", context, fullName );
        }

        /// <summary>
        /// Returns the string "[context]fullName" or "fullName" alone if context is null or empty.
        /// </summary>
        /// <param name="fullName">Object name without context prefix.</param>
        /// <param name="context">Context prefix.</param>
        /// <returns>Never null (when both <paramref name="fullName"/> and <paramref name="context"/> are null, <see cref="String.Empty"/> is returned).</returns>
        public static string FormatContextPrefix( string fullName, string context )
        {
            return String.IsNullOrEmpty( context ) ? (fullName == null ? String.Empty : fullName) : String.Format( "[{0}]{1}", context, fullName );
        }

    }
}
