using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public static class DefaultContextLocNaming
    {
        const char _locNameSeparator = '^';
        const char _locPathChar = '-';
        static readonly char[] _nameStartChars = new[] { _locNameSeparator, ']' };

        /// <summary>
        /// Returns the correct full name like "[context]location^name". 
        /// Correctly handle null context, location or name.
        /// </summary>
        /// <returns>Never null.</returns>
        public static string Format( string context, string location, string name )
        {
            if( context == null )
            {
                if( location == null ) return name ?? String.Empty;
                return location + _locNameSeparator + name;
            }
            if( location == null ) return '[' + context + ']' + name;
            return '[' + context + ']' + location + _locNameSeparator + name;
        }

        #region GetName

        /// <summary>
        /// Gets the name part of the ContextLoc string.
        /// </summary>
        /// <param name="input">The full name. Can not be null.</param>
        /// <returns>Name part in the input string.</returns>
        public static string GetName( string input )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            return DoGetName( input, 0, input.Length );
        }

        /// <summary>
        /// Gets the name part of the ContextLoc string.
        /// </summary>
        /// <param name="input">The full name. Can not be null.</param>
        /// <param name="startIndex">Index of the start in the <paramref name="input"/> to consider (up to the end of the input string).</param>
        /// <returns>Name part in the input string.</returns>
        public static string GetName( string input, int startIndex )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            int len = input.Length - startIndex;
            if( startIndex < 0 || len < 0 ) throw new ArgumentOutOfRangeException( "startIndex" );
            return DoGetName( input, startIndex, len );
        }

        /// <summary>
        /// Gets the name part of the ContextLoc string.
        /// </summary>
        /// <param name="input">The full name. Can not be null.</param>
        /// <param name="startIndex">Index of the start in the <paramref name="input"/> to consider.</param>
        /// <param name="count">Number of characters starting at <paramref name="startIndex"/> in the <paramref name="input"/> to consider.</param>
        /// <returns>Name part in the input string.</returns>
        public static string GetName( string input, int startIndex, int count )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            if( startIndex + count >= input.Length ) throw new ArgumentOutOfRangeException( "count" );
            return DoGetName( input, startIndex, count );
        }

        static string DoGetName( string input, int startIndex, int count )
        {
            int iName = input.LastIndexOfAny( _nameStartChars, startIndex + count - 1, count );
            if( iName < 0 ) iName = startIndex;
            else count -= (iName - startIndex);
            return input.Substring( iName, count );
        }
        #endregion


        #region NameStartsWith

        /// <summary>
        /// Checks whether a prefix starts the name part of the ContextLoc string.
        /// </summary>
        /// <param name="input">The input string. Can not be null.</param>
        /// <param name="prefix">Prefix to check.</param>
        /// <returns>True if the name starts with the given prefix.</returns>
        public static bool NameStartsWith( string input, string prefix )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            return DoNameStartsWith( input, 0, input.Length, prefix );
        }

        /// <summary>
        /// Checks whether a prefix starts the name part of the ContextLoc string.
        /// </summary>
        /// <param name="input">The input string. Can not be null.</param>
        /// <param name="startIndex">Index of the start in the <paramref name="input"/> to consider (up to the end of the input string).</param>
        /// <param name="prefix">Prefix to check.</param>
        /// <returns>True if the name starts with the given prefix.</returns>
        public static bool NameStartsWith( string input, int startIndex, string prefix )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            int len = input.Length - startIndex;
            if( startIndex < 0 || len < 0 ) throw new ArgumentOutOfRangeException( "startIndex" );
            return DoNameStartsWith( input, startIndex, len, prefix );
        }

        /// <summary>
        /// Checks whether a prefix starts the name part of the ContextLoc string.
        /// </summary>
        /// <param name="input">The input string. Can not be null.</param>
        /// <param name="startIndex">Index of the start in the <paramref name="input"/> to consider.</param>
        /// <param name="count">Number of characters starting at <paramref name="startIndex"/> in the <paramref name="input"/> to consider.</param>
        /// <param name="prefix">Prefix to check.</param>
        /// <returns>True if the name starts with the given prefix.</returns>
        public static bool NameStartsWith( string input, int startIndex, int count, string prefix )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            if( startIndex + count >= input.Length ) throw new ArgumentOutOfRangeException( "count" );
            return DoNameStartsWith( input, startIndex, count, prefix );
        }

        static bool DoNameStartsWith( string input, int startIndex, int count, string prefix )
        {
            if( String.IsNullOrEmpty( prefix ) ) return true;
            int iName = input.LastIndexOfAny( _nameStartChars, startIndex + count - 1, count );
            if( iName < 0 ) iName = startIndex;
            return String.CompareOrdinal( input, iName, prefix, 0, prefix.Length ) == 0;
        }

        #endregion

        #region AddNamePrefix

        /// <summary>
        /// Prepends a prefix to the name part of the ContextLoc string.
        /// </summary>
        /// <param name="input">The original string. Can not be null.</param>
        /// <param name="namePrefix">Prefix to add to the name. Must not be null nor empty.</param>
        /// <returns>Modified input so that the name part is prefixed.</returns>
        public static string AddNamePrefix( string input, string namePrefix )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            return DoAddNamePrefix( input, 0, input.Length, namePrefix );
        }

        /// <summary>
        /// Prepends a prefix to the name part of the ContextLoc string.
        /// </summary>
        /// <param name="input">The original string. Can not be null.</param>
        /// <param name="startIndex">Index of the start in the <paramref name="input"/> to consider (up to the end of the input string).</param>
        /// <param name="namePrefix">Prefix to add to the name. Must not be null nor empty.</param>
        /// <returns>Modified input so that the name part is prefixed.</returns>
        public static string AddNamePrefix( string input, int startIndex, string namePrefix )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            int len = input.Length - startIndex;
            if( startIndex < 0 || len < 0 ) throw new ArgumentOutOfRangeException( "startIndex" );
            return DoAddNamePrefix( input, startIndex, len, namePrefix );
        }

        /// <summary>
        /// Prepends a prefix to the name part of the ContextLoc string.
        /// </summary>
        /// <param name="input">The original string. Can not be null.</param>
        /// <param name="startIndex">Index of the start in the <paramref name="input"/> to consider.</param>
        /// <param name="count">Number of characters starting at <paramref name="startIndex"/> in the <paramref name="input"/> to consider.</param>
        /// <param name="namePrefix">Prefix to add to the name. Must not be null nor empty.</param>
        /// <returns>Modified input so that the name part is prefixed.</returns>
        public static string AddNamePrefix( string input, int startIndex, int count, string namePrefix )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            if( startIndex + count >= input.Length ) throw new ArgumentOutOfRangeException( "count" );
            return DoAddNamePrefix( input, startIndex, count, namePrefix );
        }

        static string DoAddNamePrefix( string input, int startIndex, int count, string namePrefix )
        {
            if( String.IsNullOrEmpty( namePrefix ) ) throw new ArgumentException( "namePrefix" );
            int iName = input.LastIndexOfAny( _nameStartChars, --startIndex + count, count );
            if( iName < 0 ) iName = startIndex;
            return input.Insert( ++iName, namePrefix );
        }
        #endregion

        #region TryParse

        /// <summary>
        /// Extracts Context, Location &amp; Name informatin from a given string: [context]location^name. 
        /// When no context nor location exists, they are null and name is the original unchanged string.
        /// </summary>
        /// <param name="input">The string to parse. Can not be null.</param>
        /// <param name="context">Output context if found. Null if not found.</param>
        /// <param name="location">Output location if found. Null if not found.</param>
        /// <param name="name">Output name. Never null.</param>
        /// <returns>True on success, false on error (context, location are null and name is empty in this case).</returns>
        public static bool TryParse( string input, out string context, out string location, out string name )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            return DoTryParse( input, 0, input.Length, out context, out location, out name );
        }

        public static bool TryParse( string input, int startIndex, out string context, out string location, out string name )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            return DoTryParse( input, startIndex, input.Length - startIndex, out context, out location, out name );
        }

        public static bool TryParse( string input, int startIndex, int count, out string context, out string location, out string name )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            if( startIndex + count >= input.Length ) throw new ArgumentOutOfRangeException( "count" );
            return DoTryParse( input, startIndex, count, out context, out location, out name );
        }

        private static bool DoTryParse( string input, int startIndex, int count, out string context, out string location, out string name )
        {
            context = null;
            location = null;
            name = String.Empty;
            if( count == 0 ) return true;
            if( input[startIndex] == '[' )
            {
                int idxEnd = input.IndexOf( ']', ++startIndex, --count );
                if( idxEnd < 0 ) return false;
                int lenContext = idxEnd - startIndex;
                context = input.Substring( startIndex, lenContext );
                startIndex = idxEnd + 1;
                count -= lenContext+1;
            }
            if( count > 0 )
            {
                int idxEnd = input.LastIndexOf( _locNameSeparator, startIndex+count-1, count );
                if( idxEnd >= 0 )
                {
                    int lenLoc = idxEnd - startIndex;
                    location = input.Substring( startIndex, lenLoc );
                    startIndex = idxEnd + 1;
                    count -= lenLoc + 1;
                }
            }
            name = input.Substring( startIndex, count );
            return true;
        }

        #endregion

        #region Resolve

        public static string Resolve( string input, string curContext, string curLoc, bool throwError = true )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            return DoResolve( input, 0, input.Length, curContext, curLoc, throwError );
        }

        public static string Resolve( string input, int startIndex, string curContext, string curLoc, bool throwError = true )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            return DoResolve( input, startIndex, input.Length - startIndex, curContext, curLoc, throwError );
        }

        public static string Resolve( string input, int startIndex, int count, string curContext, string curLoc, bool throwError = true )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            if( startIndex + count >= input.Length ) throw new ArgumentOutOfRangeException( "count" );
            return DoResolve( input, startIndex, count, curContext, curLoc, throwError );
        }

        static string DoResolve( string input, int startIndex, int count, string curContext, string curLoc, bool throwError )
        {
            string context, location, name;

            if( !DoTryParse( input, startIndex, count, out context, out location, out name ) )
            {
                if( throwError ) throw new CKException( "Syntax error in ContextLocName '{0}'.", input.Substring( startIndex, count ) );
                return null;
            }
            if( !DoCombine( curContext, curLoc, ref context, ref location, throwError ? () => input.Substring( startIndex, count ) : (Func<string>)null ) ) return null;
            string r = Format( context, location, name );
            return input.Remove( startIndex, count ).Insert( startIndex, r );
        }

        #endregion

        #region Combine

        static public bool Combine( string curContext, string curLoc, ref string context, ref string location )
        {
            return DoCombine( curContext, curLoc, ref context, ref location, null );
        }

        static bool DoCombine( string curContext, string curLoc, ref string context, ref string location, Func<string> fullNameToThrowError = null )
        {
            if( context == null ) context = curContext;
            if( location == null ) location = curLoc;
            else if( curLoc != null )
            {
                int iSep = 0;
                int idxEnd = curLoc.Length - 1;
                while( iSep < location.Length && location[iSep] == _locPathChar )
                {
                    idxEnd = curLoc.LastIndexOf( _locPathChar, idxEnd - 1 );
                    if( idxEnd < 0 )
                    {
                        if( fullNameToThrowError != null ) throw new CKException( "Invalid relative location in '{2}': '{0}' is above the root given '{1}'.", location, curLoc, fullNameToThrowError() );
                        return false;
                    }
                    ++iSep;
                }
                if( iSep > 0 )
                {
                    if( iSep == location.Length ) location = curLoc.Substring( 0, idxEnd );
                    else location = curLoc.Substring( 0, idxEnd + 1 ) + location.Substring( iSep );
                }
            }
            return true;
        }

        #endregion

    }

 }
