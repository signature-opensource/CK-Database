using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Collects <see cref="ISetupScript">scripts</see> originating from multiple sources in the context of a <see cref="ScriptTypeManager"/>.
    /// Scripts are organized in <see cref="ScriptSet"/> for each <see cref="ISetupScript.Name">setup object full name</see>.
    /// </summary>
    public class ScriptCollector : IScriptCollector
    {
        readonly ScriptTypeManager _typeManager;
        readonly Dictionary<string,ScriptSet> _scripts;
        readonly HashSet<string> _scriptSources;
        readonly ReadOnlyCollectionOnISet<string> _scriptSourcesEx;

        /// <summary>
        /// Initializes a new <see cref="ScriptCollector"/> bound to a <see cref="ScriptTypeManager"/>.
        /// </summary>
        /// <param name="typeManager"></param>
        public ScriptCollector( ScriptTypeManager typeManager )
        {
            if( typeManager == null ) throw new ArgumentNullException( "typeManager" );
            _scripts = new Dictionary<string, ScriptSet>( StringComparer.OrdinalIgnoreCase );
            _scriptSources = new HashSet<string>();
            _scriptSourcesEx = new ReadOnlyCollectionOnISet<string>( _scriptSources );
            _typeManager = typeManager;
        }

        /// <summary>
        /// Registers a <see cref="ISetupScript"/>: finds or creates a unique <see cref="ScriptSet"/> for each <see cref="ISetupScript.Name"/>.
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

        bool IScriptCollector.Add( ISetupScript script, IActivityLogger logger )
        {
            return Add( script, logger ) != null;
        }

        /// <summary>
        /// Registers a set of resources (multiple <see cref="ResSetupScript"/>) from a <see cref="ResourceLocator"/>, a full name prefix and a script source
        /// (the script source must be registered in the associated <see cref="ScriptTypeManager"/>).
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="scriptSource">The script source under which registering the <see cref="ISetupScript"/>.</param>
        /// <param name="resLoc">Resource locator.</param>
        /// <param name="context">Context identifier.</param>
        /// <param name="location">Location identifier.</param>
        /// <param name="name">Name of the object. This is used as a prefix for the resource names.</param>
        /// <param name="fileSuffix">Keeps only resources that ends with this suffix.</param>
        /// <returns>The number of scripts that have been added.</returns>
        public int AddFromResources( IActivityLogger logger, string scriptSource, ResourceLocator resLoc, string context, string location, string name, string fileSuffix )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( scriptSource == null ) throw new ArgumentNullException( "scriptSource" );
            if( resLoc == null ) throw new ArgumentNullException( "scriptSource" );
            if( name == null ) throw new ArgumentNullException( "name" );
            if( fileSuffix == null ) throw new ArgumentNullException( "fileSuffix" );
            int count = 0;
            var candidates = resLoc.GetNames( name + '.' ).Where( n => n.EndsWith( fileSuffix, StringComparison.OrdinalIgnoreCase ) );
            foreach( var s in candidates )
            {
                ParsedFileName rName;
                if( ParsedFileName.TryParse( context, location, s, resLoc, true, out rName ) )
                {
                    Add( new ResSetupScript( rName, scriptSource ), logger );
                    ++count;
                }
            }
            return count;
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
