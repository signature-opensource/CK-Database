using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class TypedScriptVector
    {
        public readonly Version Final;
        
        public readonly IReadOnlyList<CoveringScript> Scripts;
        
        public bool HasTheNoVersionScript
        {
            get { return Scripts.Count > 0 && Scripts[Scripts.Count - 1].Script.Name.Version == null; }
        }

        /// <summary>
        /// No at all scripts found.
        /// </summary>
        internal TypedScriptVector()
        {
            Scripts = ReadOnlyListEmpty<CoveringScript>.Empty;
            Final = null;
        }

        /// <summary>
        /// A (last) versioned script and an optional NoVersion script found.
        /// </summary>
        internal TypedScriptVector( ISetupScript maxScript, ISetupScript noVersion )
        {
            Debug.Assert( maxScript != null );
            Debug.Assert( noVersion == null || (noVersion.ScriptSource == maxScript.ScriptSource) );
            if( noVersion != null ) Scripts = new ReadOnlyListOnIList<CoveringScript>( new[] { new CoveringScript( maxScript ), new CoveringScript( noVersion ) } );
            else Scripts = new ReadOnlyListMono<CoveringScript>( new CoveringScript( maxScript ) );
            Final = maxScript.Name.Version;
        }

        /// <summary>
        /// A starting script, a migration script and an optional NoVersion one.
        /// </summary>
        internal TypedScriptVector( ISetupScript startingScript, ISetupScript migrationScript, ISetupScript noVersion )
        {
            Debug.Assert( startingScript != null && migrationScript != null );
            Debug.Assert( noVersion == null || (noVersion.ScriptSource == startingScript.ScriptSource) );

            var a = noVersion != null
                        ? new[] { new CoveringScript( startingScript ), new CoveringScript( migrationScript ), new CoveringScript( noVersion ) }
                        : new[] { new CoveringScript( startingScript ), new CoveringScript( migrationScript ) };

            Scripts = new ReadOnlyListOnIList<CoveringScript>( a );
            Final = migrationScript.Name.Version;
        }

        /// <summary>
        /// Only the NoVersion script found. No script with version at all.
        /// </summary>
        internal TypedScriptVector( ISetupScript noVersion )
            : this( noVersion, null )
        {
        }

        /// <summary>
        /// A list of versioned script and an optional NoVersion script found.
        /// </summary>
        internal TypedScriptVector( List<CoveringScript> scripts, ISetupScript noVersion )
        {
            Debug.Assert( scripts.All( s => s.Script.ScriptSource == scripts[0].Script.ScriptSource ) );
            Final = scripts[scripts.Count - 1].Script.Name.Version;
            if( noVersion != null ) scripts.Add( new CoveringScript( noVersion ) );
            Scripts = scripts.ToReadOnlyList();
        }
    }
}
