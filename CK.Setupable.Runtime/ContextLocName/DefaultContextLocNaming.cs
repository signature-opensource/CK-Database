#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\ContextLocName\DefaultContextLocNaming.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Exposes multiple methods that manipulate location names.
    /// </summary>
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
        /// Extracts Context, Location &amp; Name information from a given string: [context]location^name. 
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

        /// <summary>
        /// Extracts Context, Location &amp; Name information from a given string starting at a position: 
        /// [context]location^name. When no context nor location exists, they are null and name is the 
        /// original unchanged string.
        /// </summary>
        /// <param name="input">The string to parse. Can not be null.</param>
        /// <param name="startIndex">Starting index to consider in the input string.</param>
        /// <param name="context">Output context if found. Null if not found.</param>
        /// <param name="location">Output location if found. Null if not found.</param>
        /// <param name="name">Output name. Never null.</param>
        /// <returns>True on success, false on error (context, location are null and name is empty in this case).</returns>
        public static bool TryParse( string input, int startIndex, out string context, out string location, out string name )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            return DoTryParse( input, startIndex, input.Length - startIndex, out context, out location, out name );
        }

        /// <summary>
        /// Extracts Context, Location &amp; Name information from a segment of a given string: 
        /// [context]location^name. When no context nor location exists, they are null and name is the 
        /// original unchanged string.
        /// </summary>
        /// <param name="input">The string to parse. Can not be null.</param>
        /// <param name="startIndex">Starting index to consider in the input string.</param>
        /// <param name="count">Number of characters to consier.</param>
        /// <param name="context">Output context if found. Null if not found.</param>
        /// <param name="location">Output location if found. Null if not found.</param>
        /// <param name="name">Output name. Never null.</param>
        /// <returns>True on success, false on error (context, location are null and name is empty in this case).</returns>
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

        /// <summary>
        /// Updates the <paramref name="input"/> with the [curContext] and the ^curLocation if the input does not specify them.
        /// </summary>
        /// <param name="input">The input location to resolve.</param>
        /// <param name="curContext">The current context: when null, the [context] (if any) is not changed.</param>
        /// <param name="curLoc">The current location: when null, the ^location (if it exists) is not changed.</param>
        /// <param name="throwError">True to throw error if any parts have a syntax error. Otherwise returns null.</param>
        /// <returns>The updated input.</returns>
        public static string Resolve( string input, string curContext, string curLoc, bool throwError = true )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            return DoResolve( input, 0, input.Length, curContext, curLoc, throwError );
        }

        /// <summary>
        /// Updates the <paramref name="input"/> with the [curContext] and the ^curLocation of the current naming if the input does not specify them.
        /// </summary>
        /// <param name="input">The input location to resolve.</param>
        /// <param name="current">The current naming.</param>
        /// <param name="throwError">True to throw error if any parts have a syntax error. Otherwise returns null.</param>
        /// <returns>The updated input.</returns>
        public static string Resolve( string input, IContextLocNaming current, bool throwError = true )
        {
            return DoResolve( input, 0, input.Length, current.Context, current.Location, throwError );
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

        static readonly char _root = '~';
        static readonly char _sep = '.';
        static readonly char[] _sepArray = new char[] { '.' };

        /// <summary>
        /// Combines a name with a base name. If the name starts with a dot, it is always relative to the base name, '.~' or '.~.' goes up one 
        /// level, '.~.~' or '.~.~.' goes up for two levels, etc.
        /// When the name starts with '~', the base name is ignored: the name is considered as rooted.
        /// When the name is "normal", base name is considered by default as a namespace: setting <paramref name="baseIsNamespace"/> to false
        /// acts as if it was the <see cref="GetNamespace"/> of this base name. 
        /// </summary>
        /// <param name="name">Name to be combined. Can be null or empty.</param>
        /// <param name="baseName">Base name. Can be null or empty.</param>
        /// <param name="baseIsNamespace">False to skip the base name suffix (considering only its namespace) when name is "normal".</param>
        /// <returns>The combined name.</returns>
        public static string CombineNamePart( string name, string baseName, bool baseIsNamespace = true )
        {
            string result;
            if( TryCombineNamePart( name, baseName, out result, baseIsNamespace ) ) return result;
            throw new ArgumentException( String.Format( "Unable to combine '{0}' with '{1}'.", name, baseName ) );
        }

        /// <summary>
        /// Tries to combine a name with a base name. If the name starts with a dot, it is always relative to the base name, '.~' or '.~.' goes up one 
        /// level, '.~.~' or '.~.~.' goes up for two levels, etc.
        /// When the name starts with '~', the base name is ignored: the name is considered as rooted.
        /// When the name is "normal", base name is considered by default as a namespace: setting <paramref name="baseIsNamespace"/> to false
        /// acts as if it was the <see cref="GetNamespace"/> of this base name. 
        /// </summary>
        /// <param name="name">Name to be combined. Can be null or empty.</param>
        /// <param name="baseName">Base name. Can be null or empty.</param>
        /// <param name="baseIsNamespace">False to skip the base name suffix (considering only its namespace) when name is "normal".</param>
        /// <param name="result">The combined name.</param>
        /// <returns>False on error.</returns>
        public static bool TryCombineNamePart( string name, string baseName, out string result, bool baseIsNamespace = true )
        {
            if( baseName == null ) baseName = String.Empty;
            result = null;
            if( String.IsNullOrEmpty( name ) )
            {
                result = baseIsNamespace ? baseName : GetNamespace( baseName );
                return true;
            }
            if( name[0] == _root )
            {
                result = name.Substring( 1 );
                return true;
            }
            if( name[0] == _sep )
            {
                if( name.Length == 1 )
                {
                    result = baseName;
                    return true;
                }
                int iStart = 1;
                while( name[iStart] == _root )
                {
                    if( ++iStart == name.Length ) break;
                    if( name[iStart] != _sep ) return false;
                    if( ++iStart == name.Length ) break;
                }
                if( iStart == 1 )
                {
                    if( baseName.Length == 0 ) result = name.Substring( 1 );
                    else result = baseName + name;
                    return true;
                }
                int nbToSkip = iStart >> 1;
                int startIndex = baseName.Length;
                while( nbToSkip > 0 )
                {
                    if( startIndex <= 0 ) return false;
                    startIndex = baseName.LastIndexOfAny( _sepArray, startIndex - 1 );
                    --nbToSkip;
                }
                result = name.Substring( iStart );
                if( startIndex > 0 )
                {
                    if( result.Length > 0 ) ++startIndex;
                    result = baseName.Substring( 0, startIndex ) + result;
                }
                return true;
            }
            if( baseName.Length == 0 ) result = name;
            else if( baseIsNamespace ) result = baseName + '.' + name;
            else
            {
                int idx = baseName.LastIndexOfAny( _sepArray );
                if( idx < 0 ) result = name;
                else result = baseName.Substring( 0, idx + 1 ) + name;
            }
            return true;
        }

        /// <summary>
        /// Ensures that <paramref name="name"/> has a namespace (ie. it contains at least one dot).
        /// If not and if <paramref name="defaultNamespace"/> is not null nor empty, "defaultNamespace.name" is returned.
        /// </summary>
        /// <param name="name">The name to check. Must not bu null nor empty.</param>
        /// <param name="defaultNamespace">Namespace to prepend if name has no namespace.</param>
        /// <returns>The unchanged name or "defaultNamespace.name".</returns>
        public static string SetDefaultNamespace( string name, string defaultNamespace )
        {
            if( String.IsNullOrEmpty( name ) ) throw new ArgumentException( "name" );
            if( defaultNamespace != null && defaultNamespace.Length > 0 )
            {
                int idx = name.IndexOfAny( _sepArray );
                if( idx < 0 ) name = defaultNamespace + '.' + name;
            }
            return name;
        }

        /// <summary>
        /// Gets the namespace of a name.
        /// </summary>
        /// <param name="name">Name with dots.</param>
        /// <returns>Prefix up to the last dot or the empty string if there is no dots.</returns>
        public static string GetNamespace( string name )
        {
            if( name == null ) return String.Empty;
            int idx = name.LastIndexOfAny( _sepArray );
            if( idx < 0 ) return String.Empty;
            return name.Substring( 0, idx );
        }

        /// <summary>
        /// Extracts and returrns the namespace (never null, can be empty) and the leaf name (never null, can be empty if and only if
        /// the given name is null or empty).
        /// </summary>
        /// <param name="name">Name to split.</param>
        /// <param name="leafName">Leaf name. Never null.</param>
        /// <returns>The namespace. Never null, empty if there is no namespace.</returns>
        public static string SplitNamespace( string name, out string leafName )
        {
            leafName = name;
            if( String.IsNullOrEmpty( name ) ) return String.Empty;
            int idx = name.LastIndexOfAny( _sepArray );
            if( idx < 0 ) return String.Empty;
            leafName = name.Substring( idx + 1 );
            return name.Substring( 0, idx );
        }

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
