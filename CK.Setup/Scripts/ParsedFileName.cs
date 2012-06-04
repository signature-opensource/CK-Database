using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Offers <see cref="Parse"/> and <see cref="TryParse"/> or <see cref="CodeCreate"/> factory methods.
    /// </summary>
    public class ParsedFileName
    {
        string _fileName;
        object _extraPath;
        string _fullName;
        Version _fromVersion;
        Version _version;
        SetupStep _step;
        bool _isContent;

        ParsedFileName( string fileName, object extraPath, string name, Version f, Version v, SetupStep step, bool isContent )
        {
            Debug.Assert( f == null || (v != null && f != v), "from ==> version && from != version" );
            _fileName = fileName;
            _extraPath = extraPath;
            _fullName = name;
            _fromVersion = f;
            _version = v;
            _step = step;
            _isContent = isContent;
        }

        /// <summary>
        /// Gets the name of the container. Not null nor empty.
        /// </summary>
        public string ContainerFullName
        {
            get { return _fullName; }
        }

        /// <summary>
        /// Gets the original file name (including its extension if any).
        /// Not null nor empty.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
        }

        /// <summary>
        /// Gets the path (the prefix for a string or any context data that enables to locate the reource). 
        /// This information is not processed and is passed as-is from <see cref="TryParse"/> and <see cref="Parse"/> parameter.
        /// It can be null at this level. It is up to the <see cref="ISetupScript"/> that wraps it to exploit it.
        /// </summary>
        public object ExtraPath
        {
            get { return _extraPath; }
        }

        /// <summary>
        /// Gets the initial version extracted from the <see cref="FileName"/> if it is a migration script.
        /// Null if this is not a migration script.
        /// </summary>
        public Version FromVersion
        {
            get { return _fromVersion; }
        }

        /// <summary>
        /// Gets whether this is a migration script (<see cref="FromVersion"/> is not null and <see cref="Version"/> is greater than it).
        /// </summary>
        public bool IsUpgradeScript
        {
            get { return _fromVersion != null && _fromVersion < _version; }
        }

        /// <summary>
        /// Gets whether this is a downgrade script (<see cref="FromVersion"/> is not null and <see cref="Version"/> is smaller than it).
        /// </summary>
        public bool IsDowngradeScript
        {
            get { return _fromVersion != null && _fromVersion > _version; }
        }

        /// <summary>
        /// Gets the version extracted from the <see cref="FileName"/>.
        /// Null if no version at all is specified (this is a "no version" script that should always be applied last).
        /// </summary>
        public Version Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Gets the <see cref="SetupStep"/>.
        /// </summary>
        public SetupStep SetupStep
        {
            get { return _step; }
        }

        /// <summary>
        /// Gets whether the <see cref="FileName"/> applies to the content of the container (must be called after 
        /// content elements setup).
        /// </summary>
        public bool IsContent
        {
            get { return _isContent; }
        }

        /// <summary>
        /// Gets the combination of <see cref="P:SetupStep"/> and <see cref="IsContent"/> as a <see cref="SetupCallContainerStep"/>.
        /// </summary>
        public SetupCallContainerStep CallContainerStep
        {
            get
            {
                if( _step == SetupStep.None ) return SetupCallContainerStep.None;
                if( _step == SetupStep.Init ) return _isContent ? SetupCallContainerStep.InitContent : SetupCallContainerStep.Init;
                if( _step == SetupStep.Install ) return _isContent ? SetupCallContainerStep.InstallContent : SetupCallContainerStep.Install;
                return _isContent ? SetupCallContainerStep.SettleContent : SetupCallContainerStep.Settle;
            }
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
        /// Calls <see cref="TryParse"/> and throws a <see cref="FormatException"/> if it 
        /// fails to parse the given file name.
        /// </summary>
        /// <param name="fileName">The file name. Should not start with a path (otherwise the path will appear in the <see cref="Name"/>).</param>
        /// <param name="extraPath">Path part (prefix) of the <paramref name="fileName"/>.</param>
        /// <param name="hasExtension">True to ignore the trailing extension (.xxx). False if the <paramref name="fileName"/> does not end with an extension.</param>
        /// <param name="result">The parsed result.</param>
        static public ParsedFileName Parse( string fileName, string extraPath, bool hasExtension )
        {
            ParsedFileName r;
            if( !TryParse( fileName, extraPath, hasExtension, out r ) ) throw new FormatException( "Invalid file name: " + fileName );
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
        /// <param name="fileName">The file name. Should not start with a path (otherwise the path will appear in the <see cref="Name"/>).</param>
        /// <param name="extraPath">Path part (context dependant data) of the <paramref name="fileName"/>.</param>
        /// <param name="hasExtension">
        /// True to ignore the trailing extension (.xxx). False if the <paramref name="fileName"/> does not end with an extension.
        /// The <see cref="FileName"/> will contain the extension.
        /// </param>
        /// <param name="result">The parsed result or null.</param>
        /// <returns>True on success, false if the <paramref name="fileName"/> is not valid.</returns>
        static public bool TryParse( string fileName, object extraPath, bool hasExtension, out ParsedFileName result )
        {
            result = null;
            if( String.IsNullOrEmpty( fileName ) ) return false;
            string name = hasExtension ? fileName.Remove( fileName.LastIndexOf( '.' ) ) : fileName;
            if( name.Length == 0 ) return false;

            Version f = null;
            Version v = null;
            SetupStep step = SetupStep.None;
            bool isContent = false;
            Match m = _rVersion.Match( name );
            if( m.Success )
            {
                if( !Version.TryParse( m.Groups[1].Value, out v ) ) return false;
                if( m.Groups[2].Length > 0 && !Version.TryParse( m.Groups[2].Value, out f ) ) return false;
                name = name.Remove( m.Index );
            }
            if( name.Length == 0 ) return false;
            // from version to itself: rejects it.
            if( f != null && f == v ) return false;
            if( name.EndsWith( ".Init", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupStep.Init;
                name = name.Remove( name.Length - 5 );
            }
            else if( name.EndsWith( ".InitContent", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupStep.Init;
                isContent = true;
                name = name.Remove( name.Length - 12 );
            }
            else if( name.EndsWith( ".Install", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupStep.Install;
                name = name.Remove( name.Length - 8 );
            }
            else if( name.EndsWith( ".InstallContent", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupStep.Install;
                isContent = true;
                name = name.Remove( name.Length - 15 );
            }
            else if( name.EndsWith( ".Settle", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupStep.Settle;
                name = name.Remove( name.Length - 7 );
            }
            else if( name.EndsWith( ".SettleContent", StringComparison.OrdinalIgnoreCase ) )
            {
                step = SetupStep.Settle;
                isContent = true;
                name = name.Remove( name.Length - 14 );
            }
            if( name.Length == 0 ) return false;
            result = new ParsedFileName( fileName, extraPath, name, f, v, step, isContent );
            return true; 
        }

        public override string ToString()
        {
            return FileName;
        }

    }
}
