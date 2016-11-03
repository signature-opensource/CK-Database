#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\ParsedFileName.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using CK.Core;
using System.Runtime.CompilerServices;

namespace CK.Setup
{
    /// <summary>
    /// Encapsulation of a file name associated to a setup object. It handles all the steps (<see cref="SetupCallGroupStep.InitContent"/>)
    /// and versions (for <see cref="IVersionedItem"/>) but can be used for simple <see cref="IDependentItem"/>.
    /// Offers <see cref="Parse"/>, <see cref="TryParse"/> and <see cref="CreateFromSource"/> factory methods.
    /// </summary>
    public class ParsedFileName : IContextLocNaming
    {
        string _fileName;
        object _extraPath;
        string _context;
        string _loc;
        string _name;
        string _extension;
        string _transformArg;
        string _fullName;
        Version _fromVersion;
        Version _version;
        SetupCallGroupStep _step;

        private ParsedFileName( string fileName, object extraPath, string context, string location, string name, string extension, Version f, Version v, SetupCallGroupStep step )
        {
            Debug.Assert( f == null || (v != null && f != v), "from ==> version && from != version" );
            Debug.Assert( !string.IsNullOrEmpty( extension ) );
            _fileName = fileName;
            _extraPath = extraPath;
            _context = context;
            _loc = location;
            _name = name;
            _fullName = DefaultContextLocNaming.Format( _context, _loc, _name );
            _fromVersion = f;
            _version = v;
            _extension = extension;
            _step = step;
        }

        private ParsedFileName( string fileName, object extraPath, string context, string location, string name, string extension, string transformArg )
        {
            Debug.Assert( !string.IsNullOrEmpty( extension ) );
            _fileName = fileName;
            _extraPath = extraPath;
            _context = context;
            _loc = location;
            _name = name;
            _transformArg = transformArg;
            _fullName = DefaultContextLocNaming.Format( _context, _loc, _name );
            _extension = extension;
            _step = SetupCallGroupStep.None;
        }

        private ParsedFileName( string fileName, object extraPath, IContextLocNaming locName, string extension )
        {
            Debug.Assert( !string.IsNullOrEmpty( extension ) );
            _fileName = fileName;
            _extraPath = extraPath;
            _context = locName.Context;
            _loc = locName.Location;
            _name = locName.Name;
            _transformArg = locName.TransformArg;
            _fullName = locName.FullName;
            _extension = extension;
            _step = SetupCallGroupStep.None;
        }

        /// <summary>
        /// Gets the context identifier (see <see cref="DefaultContextLocNaming"/>).
        /// </summary>
        public string Context => _context; 

        /// <summary>
        /// Gets the location (see <see cref="DefaultContextLocNaming"/>).
        /// </summary>
        public string Location => _loc; 

        /// <summary>
        /// Gets the name of the item without its <see cref="Context"/> nor <see cref="Location"/>. Not null nor empty.
        /// </summary>
        public string Name => _name; 

        /// <summary>
        /// Gets the source full name (suffix enclosed in parenthesis) if it exists.
        /// </summary>
        public string TransformArg => _transformArg;

        /// <summary>
        /// Gets the name of the item with its context, location and name. Not null nor empty.
        /// </summary>
        public string FullName => _fullName;

        /// <summary>
        /// Gets the original file name (including its extension and [Context] prefix if any) without any normalization.
        /// Not null nor empty.
        /// </summary>
        public string FileName => _fileName;

        /// <summary>
        /// Gets the extension without dot prefix.
        /// Never null nor empty: an extension must always exist.
        /// </summary>
        public string Extension => _extension;

        /// <summary>
        /// Gets the path (the prefix for a string or any contextual data that enables to locate the resource). 
        /// This information is not processed and is passed as-is from <see cref="TryParse"/> and <see cref="Parse"/> parameter.
        /// It can be null at this level. It is up to the <see cref="ISetupScript"/> that wraps it to exploit it.
        /// </summary>
        public object ExtraPath => _extraPath; 

        /// <summary>
        /// Gets the initial version extracted from the <see cref="FileName"/> if it is a migration script.
        /// Null if this is not a migration script.
        /// </summary>
        public Version FromVersion => _fromVersion; 

        /// <summary>
        /// Gets whether this is a migration script (<see cref="FromVersion"/> is not null and <see cref="Version"/> is greater than it).
        /// </summary>
        public bool IsUpgradeScript => _fromVersion != null && _fromVersion < _version; 

        /// <summary>
        /// Gets whether this is a downgrade script (<see cref="FromVersion"/> is not null and <see cref="Version"/> is smaller than it).
        /// </summary>
        public bool IsDowngradeScript => _fromVersion != null && _fromVersion > _version; 

        /// <summary>
        /// Gets the version extracted from the <see cref="FileName"/>. Null if no version at all is specified: this is the "no version" script. See remarks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The "no version" script should always be executed.
        /// This offers a coherency between versioned and non-versioned items and enables versioned items to behave like a non-versioned one.
        /// </para>
        /// <para>
        /// If the item is not an <see cref="IVersionedItem"/> or if its <see cref="IVersionedItem.Version"/> is null, this is the only script that applies.
        /// For versioned items (when <see cref="IVersionedItem"/> is implemented and <see cref="IVersionedItem.Version"/> is not null) the NoVersion script will 
        /// be applied after the last versioned script if it exists (be it a <see cref="IsUpgradeScript"/> or not).
        /// </para>
        /// </remarks>
        public Version Version => _version; 

        /// <summary>
        /// Gets the <see cref="SetupCallGroupStep"/>.
        /// </summary>
        public SetupCallGroupStep SetupStep => _step;

        /// <summary>
        /// Gets the combination of <see cref="P:SetupStep"/> and <see cref="IsContent"/> as a <see cref="SetupCallGroupStep"/>.
        /// </summary>
        public SetupCallGroupStep CallContainerStep => _step;

        /// <summary>
        /// Checks if this file must be taken into account to upgrade from the given version.
        /// </summary>
        /// <param name="from">The current version to start from.</param>
        /// <param name="includeNoVersionScript">True to accept the script where <see cref="Version"/> is null.</param>
        /// <returns>True if this file can be applied.</returns>
        public bool BelongsToUpgradeFrom( Version from, bool includeNoVersionScript = false )
        {
            // If it is a migration...
            if( _fromVersion != null )
            {
                // ..and an upgrade...
                if( _fromVersion < _version )
                {
                    return _fromVersion >= from;
                }
            }
            // The "no version" belongs to all upgrades.
            return includeNoVersionScript && _version == null;
        }

        /// <summary>
        /// Creates a <see cref="ParsedFileName"/> from source code.
        /// </summary>
        /// <param name="locName">The context-location-name for which a a <see cref="ParsedFileName"/> must be created.</param>
        /// <param name="extension">The extension must not be null, empty or starts with a '.' dot.</param>
        /// <param name="file">Automatically set the file source name. The path is ignored: only the filename with its extension is kept.</param>
        /// <param name="line">Automatically set the source line number.</param>
        /// <returns>A parsed file name.</returns>
        public static ParsedFileName CreateFromSource( IContextLocNaming locName, string extension, [CallerFilePath]string file = null, [CallerLineNumber] int line = 0 )
        {
            if( locName == null ) throw new ArgumentNullException( nameof( locName ) );
            if( extension == null || extension.Length == 0 || extension[0] == '.' ) throw new ArgumentException();
            return new ParsedFileName( $"{Path.GetFileName( file )}@{line}", "source:", locName, extension );
        }

        /// <summary>
        /// Calls <see cref="TryParse"/> and throws a <see cref="FormatException"/> if it 
        /// fails to parse the given file name.
        /// </summary>
        /// <param name="curContext">Current context identifier. It will be used as the <see cref="Context"/> if <paramref name="fileName"/> does not contain it. Null if no current context exist.</param>
        /// <param name="curLoc">Current location identifier. It will be used as the <see cref="Location"/> if <paramref name="fileName"/> does not contain a location. Null if no current location exist.</param>
        /// <param name="fileName">The file name. Should not start with a path.</param>
        /// <param name="extraPath">Path part (prefix) of the <paramref name="fileName"/>.</param>
        /// <param name="extension">Explicit extension (without the dot prefix). Null extracts the extension from the fileName.</param>
        /// <returns>The parsed result.</returns>
        static public ParsedFileName Parse( string curContext, string curLoc, string fileName, object extraPath, string extension = null )
        {
            ParsedFileName r;
            if( !TryParse( curContext, curLoc, fileName, extraPath, out r, extension ) ) throw new FormatException( "Invalid file name '" + fileName + "' in '" + extraPath + "'." );
            return r;
        }

        static Regex _rVersion = new Regex( @"\.((?<2>\d+\.\d+\.\d+)\.to\.)?(?<1>\d+\.\d+\.\d+)$", RegexOptions.CultureInvariant | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase );

        /// <summary>
        /// Attempts to parse the given path that can have an extension and may ends with
        /// a <see cref="SetupStep"/> (".Init", ".Install" or ."Settle") related to the Content 
        /// or not (".InitContent", ".InstallContent" or ."SettleContent"), and an optional 
        /// trailing version with 3 numbers (ends with ".X.Y.Z") or a migration path (ends 
        /// with ".U.V.W.to.X.Y.Z").
        /// </summary>
        /// <param name="curContext">Current context identifier. It will be used as the <see cref="Context"/> if <paramref name="fileName"/> does not contain it. Null if no current context exist.</param>
        /// <param name="curLoc">Current location identifier. It will be used as the <see cref="Location"/> if <paramref name="fileName"/> does not contain a location. Null if no current location exist.</param>
        /// <param name="fileName">The file name. Should not start with a path.</param>
        /// <param name="extraPath">Path part (context dependent data) of the <paramref name="fileName"/>.</param>
        /// <param name="result">The parsed result or null.</param>
        /// <param name="extension">Explicit extension (without the dot prefix). Null extracts the extension from the fileName.</param>
        /// <returns>True on success, false if the <paramref name="fileName"/> is not valid.</returns>
        /// <remarks>
        /// The context and location are optional since they can be given (<paramref name="curContext"/> and <paramref name="curLoc"/>). 
        /// If the <paramref name="fileName"/> contains such context or location, they take precedence over the 2 parameters.
        /// </remarks>
        static public bool TryParse( string curContext, string curLoc, string fileName, object extraPath, out ParsedFileName result, string extension = null )
        {
            result = null;
            if( string.IsNullOrEmpty( fileName ) ) return false;

            // The n is the future Name.
            string n = fileName;
            if( extension == null )
            {
                int idxExtension = fileName.LastIndexOf( '.' );
                if( idxExtension <= 0 || idxExtension == fileName.Length - 1 ) return false;
                extension = fileName.Substring( idxExtension + 1 );
                n = fileName.Remove( idxExtension );
            }
            else
            {
                if( extension.Length == 0 || extension[0] == '.' ) return false;
            }
            Debug.Assert( n.Length > 0 );
            
            string context, location, source;
            if( !DefaultContextLocNaming.TryParse( n, out context, out location, out n, out source ) ) return false;
            if( !DefaultContextLocNaming.Combine( curContext, curLoc, ref context, ref location ) ) return false;
            if( source != null )
            {
                if( (source = DefaultContextLocNaming.Resolve( source, curContext, curLoc, throwError: false )) == null )
                {
                    return false;
                }
                result = new ParsedFileName( fileName, extraPath, context, location, n, extension, source );
                return true;
            }
            Version f = null;
            Version v = null;
            SetupCallGroupStep step = SetupCallGroupStep.None;
            Match m = _rVersion.Match( n );
            if( m.Success )
            {
                if( !Version.TryParse( m.Groups[1].Value, out v ) ) return false;
                if( m.Groups[2].Length > 0 && !Version.TryParse( m.Groups[2].Value, out f ) ) return false;
                n = n.Remove( m.Index );
            }
            if( n.Length == 0 ) return false;
            // from version to itself: rejects it.
            if( f != null && f == v ) return false;
            if( n.EndsWith( ".Init", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupCallGroupStep.Init;
                n = n.Remove( n.Length - 5 );
            }
            else if( n.EndsWith( ".InitContent", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupCallGroupStep.InitContent;
                n = n.Remove( n.Length - 12 );
            }
            else if( n.EndsWith( ".Install", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupCallGroupStep.Install;
                n = n.Remove( n.Length - 8 );
            }
            else if( n.EndsWith( ".InstallContent", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupCallGroupStep.InstallContent;
                n = n.Remove( n.Length - 15 );
            }
            else if( n.EndsWith( ".Settle", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupCallGroupStep.Settle;
                n = n.Remove( n.Length - 7 );
            }
            else if( n.EndsWith( ".SettleContent", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupCallGroupStep.SettleContent;
                n = n.Remove( n.Length - 14 );
            }
            if( n.Length == 0 ) return false;
            result = new ParsedFileName( fileName, extraPath, context, location, n, extension, f, v, step );
            return true; 
        }

        public override string ToString() => FileName;

    }
}
