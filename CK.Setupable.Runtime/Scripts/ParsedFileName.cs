#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\ParsedFileName.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.Setup
{
    /// <summary>
    /// Encapsulation of a file name associated to a setup object. It handles all the steps (<see cref="SetupStep"/>)
    /// and versions (for <see cref="IVersionedItem"/>) but can be used for simple <see cref="IDependentItem"/>.
    /// Offers <see cref="Parse"/>, <see cref="TryParse"/> and <see cref="CreateFromSourceCode"/> factory methods.
    /// </summary>
    public class ParsedFileName : IContextLocNaming
    {
        readonly static string _extraPathForSourceCode = "source:";
        readonly string _fileName;
        readonly object _extraPath;
        readonly string _context;
        readonly string _loc;
        readonly string _name;
        readonly string _extension;
        readonly string _transformArg;
        readonly string _fullName;
        readonly Version _fromVersion;
        readonly Version _version;
        readonly SetupCallGroupStep _step;

        ParsedFileName( string fileName, object extraPath, string context, string location, string name, string transformArg, string extension, Version f, Version v, SetupCallGroupStep step )
        {
            Debug.Assert( f == null || (v != null && f != v), "from ==> version && from != version" );
            Debug.Assert( !string.IsNullOrEmpty( extension ) );
            _fileName = fileName;
            _extraPath = extraPath;
            _context = context;
            _loc = location;
            _name = name;
            _transformArg = transformArg;
            _fullName = DefaultContextLocNaming.Format( _context, _loc, _name );
            _extension = extension;
            _fromVersion = f;
            _version = v;
            _step = step;
        }

        ParsedFileName( string fileName, object extraPath, IContextLocNaming locName, string extension, SetupCallGroupStep step, Version f, Version v )
        {
            Debug.Assert( f == null || (v != null && f != v), "from ==> version && from != version" );
            Debug.Assert( !string.IsNullOrEmpty( extension ) );
            _fileName = fileName;
            _extraPath = extraPath;
            _context = locName.Context;
            _loc = locName.Location;
            _name = locName.Name;
            _transformArg = locName.TransformArg;
            _fullName = locName.FullName;
            _extension = extension;
            _fromVersion = f;
            _version = v;
            _step = step;
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
        /// Gets whether this <see cref="ParsedFileName"/> is from source code.
        /// </summary>
        public bool IsFromSourceCode => ReferenceEquals( _extraPath, _extraPathForSourceCode );

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
        /// Computes a key that identifies this <see cref="ParsedFileName"/>: it is a combination
        /// of the <see cref="Extension"/> (or <see cref="FileName"/> if <see cref="IsFromSourceCode"/> is true), 
        /// <see cref="ParsedFileName.FullName"/>, <see cref="ParsedFileName.SetupStep"/>, 
        /// <see cref="ParsedFileName.FromVersion"/> and <see cref="ParsedFileName.Version"/>.
        /// </summary>
        /// <param name="suffix">Optional suffix to append to the key (avoids another concatenation).</param>
        /// <returns>
        /// A key that identifies this parsed file name: two scripts with this same key can not both 
        /// participate in a setup.
        /// </returns>
        public string GetScriptKey( string suffix = null )
        {
            return $"{(IsFromSourceCode ? FileName : Extension)}|{FullName}|{SetupStep}|{FromVersion}|{Version}|{suffix}";
        }

        /// <summary>
        /// Sets the extension. Not null nor empty and must not start with a '.'.
        /// </summary>
        /// <param name="newExtension">New extension.</param>
        /// <returns>A <see cref="ParsedFileName"/> with the extension.</returns>
        public ParsedFileName SetExtension( string newExtension )
        {
            if( _extension == newExtension ) return this;
            return new ParsedFileName( _fileName, _extraPath, _context, _loc, _name, _transformArg, newExtension, _fromVersion, _version, _step );
        }

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
        /// <param name="step">Optional step (when no <paramref name="version"/> is supplied, this is a "no version" script).</param>
        /// <param name="fromVersion">
        /// Optional starting version for a migration script. 
        /// When not null, <paramref name="version"/> must be supplied.
        /// </param>
        /// <param name="version">Optional version of the script.</param>
        /// <param name="file">Automatically set the file source name. The path is ignored: only the filename with its extension is kept.</param>
        /// <param name="line">Automatically set the source line number.</param>
        /// <returns>A parsed file name.</returns>
        public static ParsedFileName CreateFromSourceCode( 
            IContextLocNaming locName, 
            string extension, 
            SetupCallGroupStep step = SetupCallGroupStep.None, 
            Version fromVersion = null,
            Version version = null,
            [CallerFilePath]string file = null, 
            [CallerLineNumber] int line = 0 )
        {
            if( locName == null ) throw new ArgumentNullException( nameof( locName ) );
            if( extension == null || extension.Length == 0 || extension[0] == '.' ) throw new ArgumentException();
            if( fromVersion != null && (version == null || fromVersion == version) ) throw new ArgumentException( "from ==> version && from != version" );
            string fileName = $"{Path.GetFileName( file )}@{line}.{extension}";
            if( step != SetupCallGroupStep.None ) fileName += '-' + step.ToString();
            if( fromVersion != null ) fileName += $".{fromVersion}.to.{version}";
            else if( version != null ) fileName += $".{version}";
            return new ParsedFileName( fileName, _extraPathForSourceCode, locName, extension, step, fromVersion, version );
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

        static Regex _rSuffix = new Regex( @"(\.(?<3>Init(Content)?|Install(Content)?|Settle(Content)?))?(\.((?<2>\d+\.\d+\.\d+)\.to\.)?(?<1>\d+\.\d+\.\d+))?$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );

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
            
            string cleanName, context, location, transformArg;
            if( !DefaultContextLocNaming.TryParse( n, out context, out location, out cleanName, out transformArg ) ) return false;
            if( !DefaultContextLocNaming.Combine( curContext, curLoc, ref context, ref location ) ) return false;
            if( transformArg != null )
            {
                if( (transformArg = DefaultContextLocNaming.Resolve( transformArg, curContext, curLoc, throwError: false )) == null )
                {
                    return false;
                }
            }
            if( cleanName.Length == 0 ) return false;
            Version f = null;
            Version v = null;
            SetupCallGroupStep step = SetupCallGroupStep.None;
            Match m = _rSuffix.Match( n );
            if( m.Length > 0 )
            {
                if( m.Groups[1].Value.Length > 0 )
                {
                    if( !Version.TryParse( m.Groups[1].Value, out v ) ) return false;
                    if( m.Groups[2].Length > 0 && !Version.TryParse( m.Groups[2].Value, out f ) ) return false;
                }
                string strStep = m.Groups[3].Value;
                if( strStep.Length > 0 )
                {
                    switch( strStep[2] )
                    {
                        case 'I': case 'i': step = SetupCallGroupStep.Init; break;
                        case 'S': case 's': step = SetupCallGroupStep.Install; break;
                        default:
                            {
                                Debug.Assert( strStep[2] == 't' || strStep[2] == 'T' );
                                step = SetupCallGroupStep.Settle;
                                break;
                            }
                    }
                    if( strStep.Length > 7 ) step += 1;
                }
                if( transformArg == null ) cleanName = cleanName.Remove( cleanName.Length - m.Length );
            }
            result = new ParsedFileName( fileName, extraPath, context, location, cleanName, transformArg, extension, f, v, step );
            return true; 
        }

        /// <summary>
        /// Overridden to return the <see cref="FileName"/>.
        /// </summary>
        /// <returns>The file name.</returns>
        public override string ToString() => FileName;

    }
}
