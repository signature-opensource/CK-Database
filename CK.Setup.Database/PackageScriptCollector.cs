using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.Database
{
    public class PackageScriptCollector
    {
        Dictionary<string,PackageScriptSet> _scripts;
        HashSet<string> _scriptTypes;
        ReadOnlyCollectionOnISet<string> _scriptTypesEx;

        public PackageScriptCollector()
        {
            _scripts = new Dictionary<string, PackageScriptSet>( StringComparer.OrdinalIgnoreCase );
            _scriptTypes = new HashSet<string>();
            _scriptTypesEx = new ReadOnlyCollectionOnISet<string>( _scriptTypes );
        }

        /// <summary>
        /// Register a <see cref="PackageScriptSet"/> for a package script.
        /// The first name becomes the case-insensitive key: names with different case will
        /// be detected: a warning will be emitted into the logger and null will be returned.
        /// </summary>
        /// <param name="name">A setup script. Must be not null.</param>
        /// <param name="logger">The logger to use.</param>
        /// <returns>A script set or null if casing differ or if the script already exists in the <see cref="PackageScriptSet"/>.</returns>
        public PackageScriptSet Add( ISetupScript script, IActivityLogger logger )
        {
            if( script == null ) throw new ArgumentException( "script" );
            if( logger == null ) throw new ArgumentNullException( "logger" );

            PackageScriptSet s = _scripts.GetOrSet( script.Name.FullName, n => new PackageScriptSet( n ) );
            if( s.PackageFullName != script.Name.FullName )
            {
                logger.Warn( "Script '{0}' can not be associated to '{1}' (names are case-sensitive).", script.Name.FullName, s.PackageFullName );
                return null;
            }
            _scriptTypes.Add( script.ScriptType );
            if( !s.Add( script ) )
            {
                logger.Warn( "Script '{0}' in '{1}' for {2} is already registered. It is ignored.", script.Name.FileName, script.Name.ExtraPath, script.Name.FullName );
                return null;
            }
            return s;
        }

        /// <summary>
        /// Gets all script types that this collector contains.
        /// </summary>
        public IReadOnlyCollection<string> ScriptTypes
        {
            get { return _scriptTypesEx; }
        }

        /// <summary>
        /// Finds a <see cref="PackageScriptSet"/> associated to a package.
        /// </summary>
        /// <param name="fullName">The full name of the package.</param>
        /// <param name="caseDiffer">True if casing differ: this should be considered as an error.</param>
        /// <returns>The <see cref="PackageScriptSet"/> if it exists.</returns>
        public PackageScriptSet Find( string fullName, out bool caseDiffer )
        {
            PackageScriptSet result;
            caseDiffer = false;
            if( _scripts.TryGetValue( fullName, out result ) )
            {
                if( result.PackageFullName != fullName ) caseDiffer = true;
            }
            return result;
        }

    }
}
