using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public class ScriptCollector
    {
        Dictionary<string,ScriptSet> _scripts;
        HashSet<string> _scriptSources;
        ReadOnlyCollectionOnISet<string> _scriptSourcesEx;
        ScriptTypeManager _typeManager;

        public ScriptCollector( ScriptTypeManager typeManager )
        {
            _scripts = new Dictionary<string, ScriptSet>( StringComparer.OrdinalIgnoreCase );
            _scriptSources = new HashSet<string>();
            _scriptSourcesEx = new ReadOnlyCollectionOnISet<string>( _scriptSources );
            _typeManager = typeManager;
        }

        /// <summary>
        /// Register a <see cref="ScriptSet"/> for a package script.
        /// The first name becomes the case-insensitive key: names with different case will
        /// be detected, a warning will be emitted into the logger and null will be returned.
        /// </summary>
        /// <param name="script">A setup script. Must be not null.</param>
        /// <param name="logger">The logger to use.</param>
        /// <returns>A script set or null if casing differ or if the script already exists in the <see cref="ScriptSet"/>.</returns>
        public ScriptSet Add( ISetupScript script, IActivityLogger logger )
        {
            if( script == null ) throw new ArgumentException( "script" );
            if( logger == null ) throw new ArgumentNullException( "logger" );

            ScriptSet s = _scripts.GetOrSet( script.Name.FullName, n => new ScriptSet( n ) );
            if( s.FullName != script.Name.FullName )
            {
                logger.Warn( "Script '{0}' can not be associated to '{1}' (names are case-sensitive). It is ignored.", script.Name.FileName, s.FullName );
                return null;
            }
            ScriptSource source = _typeManager.FindSourceByName( script.ScriptSource );
            if( source == null )
            {
                logger.Warn( "Script source '{2}' is not registered. Script '{0}' for '{1}' will be ignored.", script.Name.FileName, s.FullName, script.ScriptSource );
                return null;
            }
            return s.Add( logger, source, script, _typeManager ) ? s : null;
        }

        /// <summary>
        /// Finds a <see cref="ScriptSet"/> associated to a package.
        /// </summary>
        /// <param name="fullName">The full name of the package.</param>
        /// <param name="caseDiffer">True if casing differ: this should be considered as an error.</param>
        /// <returns>The <see cref="ScriptSet"/> if it exists.</returns>
        public ScriptSet Find( string fullName, out bool caseDiffer )
        {
            ScriptSet result;
            caseDiffer = false;
            if( _scripts.TryGetValue( fullName, out result ) )
            {
                if( result.FullName != fullName ) caseDiffer = true;
            }
            return result;
        }

    }
}
