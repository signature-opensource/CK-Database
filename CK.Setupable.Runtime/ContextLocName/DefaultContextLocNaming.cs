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
        /// Returns the correct full name: "[context]location^name". 
        /// Correctly handle null context, location or name.
        /// </summary>
        /// <returns>Never null.</returns>
        public static string Format( string context, string location, string name )
        {
            if( context == null )
            {
                if( location == null ) return name ?? string.Empty;
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
            int idxOpenPar = input.IndexOf( '(', startIndex, count );
            int countLookup = idxOpenPar >= 0 ? idxOpenPar - startIndex : count;
            int iName = input.LastIndexOfAny( _nameStartChars, startIndex + countLookup - 1, countLookup );
            if( iName < 0 ) iName = startIndex;
            else count -= (iName - startIndex);
            return input.Substring( iName, count );
        }
        #endregion

        /// <summary>
        /// Gets the namespace of a name.
        /// </summary>
        /// <param name="name">Name with dots.</param>
        /// <returns>Prefix up to the last dot or the empty string if there is no dots.</returns>
        public static string GetNamespace( string name )
        {
            if( name == null ) return String.Empty;
            int idxMax = name.IndexOf( '(' );
            int idx = name.LastIndexOf( '.', idxMax >= 0 ? idxMax : name.Length - 1 );
            return idx < 0 ? string.Empty : name.Substring( 0, idx );
        }

        /// <summary>
        /// Extracts and returns the namespace (never null, can be empty) and the leaf name (never null, can be empty if and only if
        /// the given name is null or empty).
        /// </summary>
        /// <param name="name">Name to split.</param>
        /// <param name="leafName">Leaf name. Never null.</param>
        /// <returns>The namespace. Never null, empty if there is no namespace.</returns>
        public static string SplitNamespace( string name, out string leafName )
        {
            leafName = name;
            if( string.IsNullOrEmpty( name ) ) return string.Empty;
            int idxMax = name.IndexOf( '(' );
            int idx = name.LastIndexOf( '.', idxMax >= 0 ? idxMax : name.Length - 1 );
            if( idx < 0 ) return string.Empty;
            leafName = name.Substring( idx + 1 );
            return name.Substring( 0, idx );
        }

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
            if( string.IsNullOrEmpty( prefix ) ) return true;
            int iName = input.LastIndexOfAny( _nameStartChars, startIndex + count - 1, count );
            if( iName < 0 ) iName = startIndex;
            return string.CompareOrdinal( input, iName, prefix, 0, prefix.Length ) == 0;
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
        /// <param name="transformArg">Output optional full name of the transformation argument. Null if this name is not a transformation.</param>
        /// <returns>True on success, false on error (context, location are null and name is empty in this case).</returns>
        public static bool TryParse( string input, out string context, out string location, out string name, out string transformArg )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            return DoTryParse( input, 0, input.Length, out context, out location, out name, out transformArg );
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
        /// <param name="transformArg">Output optional full name of the transformation argument. Null if this name is not a transformation.</param>
        /// <returns>True on success, false on error (context, location are null and name is empty in this case).</returns>
        public static bool TryParse( string input, int startIndex, out string context, out string location, out string name, out string transformArg )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            return DoTryParse( input, startIndex, input.Length - startIndex, out context, out location, out name, out transformArg );
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
        /// <param name="transformArg">Output optional full name of the transformation argument. Null if this name is not a transformation.</param>
        /// <param name="name">Output name. Never null.</param>
        /// <returns>True on success, false on error (context, location and source are null and name is empty in this case).</returns>
        public static bool TryParse( string input, int startIndex, int count, out string context, out string location, out string name, out string transformArg )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( startIndex < 0 || startIndex > input.Length ) throw new ArgumentOutOfRangeException( "startIndex" );
            if( startIndex + count >= input.Length ) throw new ArgumentOutOfRangeException( "count" );
            return DoTryParse( input, startIndex, count, out context, out location, out name, out transformArg );
        }

        private static bool DoTryParse( 
            string input, 
            int startIndex, 
            int count, 
            out string context, 
            out string location, 
            out string name,
            out string target )
        {
            context = null;
            location = null;
            target = null;
            name = string.Empty;
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
                int idxEnd = input.IndexOf( _locNameSeparator, startIndex, count );
                if( idxEnd >= 0 )
                {
                    int lenLoc = idxEnd - startIndex;
                    location = input.Substring( startIndex, lenLoc );
                    startIndex = idxEnd + 1;
                    count -= lenLoc + 1;
                }
                if( count > 0 )
                {
                    target = ExtractTransformArg( input, startIndex, ref count );
                    if( target != null && target.Length == 0 ) return false;
                }
            }
            name = input.Substring( startIndex, count );
            return true;
        }

        /// <summary>
        /// Extracts the '(...)' suffix if it exists and contains balanced opening/closing parenthesis.
        /// An empty string is returned if the suffix is '()' (that is invalid).
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="startIndex">Start of the sustring to consider in input.</param>
        /// <param name="count">
        /// Number of characters to consider.
        /// On output, it is updated to the end of the transform argument (the last balanced parenthesis found)
        /// if there is a transform argument.
        /// </param>
        /// <returns>The transformation argument without the enclosing parenthesis.</returns>
        public static string ExtractTransformArg( string input, int startIndex, ref int count )
        {
            int idx = IndexOfTransformArgOpenPar( input, startIndex, ref count );
            return idx >= 0 ? input.Substring( idx + 1, count - idx + startIndex - 2 ) : null;
        }

        /// <summary>
        /// Gets the starting index of '(...)' suffix and updates count accordingly if it exists and 
        /// contains balanced opening/closing parenthesis.
        /// A positive index is returned even if the suffix is '()' (that is invalid).
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="startIndex">Start of the sustring to consider in input.</param>
        /// <param name="count">Number of characters to consider.</param>
        /// <returns>The index of the opening parenthesis.</returns>
        static int IndexOfTransformArgOpenPar( string input, int startIndex, ref int count )
        {
            int idxOpenPar = input.IndexOf( '(', startIndex, count );
            if( idxOpenPar >= 0 )
            {
                int max = startIndex + count;
                int nbOpen = 0;
                for( int i = idxOpenPar + 1; i < max; ++i )
                {
                    char c = input[i];
                    if( c == '(' ) ++nbOpen;
                    else if( c == ')' && --nbOpen < 0 )
                    {
                        count = i + 1 - startIndex;
                        return idxOpenPar;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Removes the (...) suffix if it exists.
        /// </summary>
        /// <param name="input">The input string (name or full name).</param>
        /// <param name="startIndex">Start of the sustring to consider in input.</param>
        /// <param name="count">Number of characters to consider.</param>
        /// <returns>The input string without transformation argument if it exists.</returns>
        public static string RemoveTransformArg( string input, int startIndex, int count )
        {
            int idx = IndexOfTransformArgOpenPar( input, startIndex, ref count );
            return idx >= 0 ? input.Substring( 0, idx ) : input;
        }

        /// <summary>
        /// Tests whether the name or full name ends with a transformation argument.
        /// This is true even if the transformation argument is empty (which is invalid).
        /// </summary>
        /// <param name="input">The name or full name. Must not be null.</param>
        /// <returns>True if and only if the input ends with a (..).</returns>
        public static bool HasTransformArg( string input )
        {
            if( input == null ) throw new ArgumentNullException( nameof( input ) );
            return input.Length > 0 && input[input.Length - 1] == ')';
        }

        /// <summary>
        /// Appends a (...) suffix.
        /// </summary>
        /// <param name="input">The input string (name or full name). Can not be null.</param>
        /// <param name="transformArg">Transorm argument. Can not be null nor empty nor whitespace.</param>
        /// <returns>The source full name without the enclosing parenthesis.</returns>
        public static string AppendTransformArg( string input, string transformArg )
        {
            if( input == null ) throw new ArgumentNullException( nameof( input ) );
            if( string.IsNullOrWhiteSpace(transformArg) ) throw new ArgumentException( "Can not be null, empty or whitespace.", nameof( transformArg ) );
            return input + "(" + transformArg + ")";
        }

        /// <summary>
        /// Helper that throws an <see cref="ArgumentException"/> if the input name or full name
        /// ends with a (...).
        /// </summary>
        /// <param name="input">Nam or full name. Can be null: no exception is thrown in this case.</param>
        public static void ThrowIfTransformArg( string input )
        {
            if( input != null && HasTransformArg( input ) ) throw new ArgumentException( "No TransformArg can be set." );
        }

        #endregion

        #region Resolve

        /// <summary>
        /// Updates the <paramref name="input"/> with the [curContext] and the curLocation^ if the input does not specify them.
        /// </summary>
        /// <param name="input">The input location to resolve.</param>
        /// <param name="curContext">The current context: when null, the [context] (if any) is not changed.</param>
        /// <param name="curLoc">The current location: when null, the location^ (if it exists) is not changed.</param>
        /// <param name="throwError">True to throw error if any parts have a syntax error. Otherwise returns null.</param>
        /// <returns>The updated input.</returns>
        public static string Resolve( string input, string curContext, string curLoc, bool throwError = true )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            return DoResolve( input, 0, input.Length, curContext, curLoc, throwError );
        }

        /// <summary>
        /// Updates the <paramref name="input"/> with the [curContext] and the curLocation^ of the current naming if the input does not specify them.
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
            string context, location, name, target;

            if( !DoTryParse( input, startIndex, count, out context, out location, out name, out target ) )
            {
                if( throwError ) throw new CKException( "Syntax error in ContextLocName '{0}'.", input.Substring( startIndex, count ) );
                return null;
            }
            if( target != null )
            {
                if( target.Length == 0 )
                {
                    if( throwError ) throw new CKException( "Syntax error in ContextLocName '{0}': invalid suffix '()'.", input.Substring( startIndex, count ) );
                    return null;
                }
                target = Resolve( target, curContext, curLoc, throwError );
                if( target == null ) return null;
                name = AppendTransformArg( RemoveTransformArg( name, 0, name.Length ), target );
            }
            if( !DoCombine( curContext, curLoc, ref context, ref location, throwError ? () => input.Substring( startIndex, count ) : (Func<string>)null ) ) return null;
            string r = Format( context, location, name );
            return input.Remove( startIndex, count ).Insert( startIndex, r );
        }

        #endregion

        #region Combine.

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
            if( baseName == null ) baseName = string.Empty;
            result = null;
            if( string.IsNullOrEmpty( name ) )
            {
                result = baseIsNamespace ? baseName : GetNamespace( baseName );
                return true;
            }
            if( name[0] == '~' )
            {
                result = name.Substring( 1 );
                return true;
            }
            if( name[0] == '.' )
            {
                if( name.Length == 1 )
                {
                    result = baseName;
                    return true;
                }
                int iStart = 1;
                while( name[iStart] == '~' )
                {
                    if( ++iStart == name.Length ) break;
                    if( name[iStart] != '.' ) return false;
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
                    startIndex = baseName.LastIndexOf( '.', startIndex - 1 );
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
                int idx = baseName.LastIndexOf( '.' );
                if( idx < 0 ) result = name;
                else result = baseName.Substring( 0, idx + 1 ) + name;
            }
            return true;
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
                        if( fullNameToThrowError != null ) throw new CKException( $"Invalid relative location in '{fullNameToThrowError()}': '{location}' is above the root given '{curLoc}'." );
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
