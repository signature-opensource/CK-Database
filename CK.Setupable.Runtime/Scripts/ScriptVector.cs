using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Contains versionned scripts related to the same object.
    /// </summary>
    public class ScriptVector
    {
        /// <summary>
        /// The final version.
        /// </summary>
        public readonly Version Final;
        
        /// <summary>
        /// The available scripts.
        /// </summary>
        public readonly IReadOnlyList<CoveringScript> Scripts;
        
        /// <summary>
        /// Gets whether the "no version" script exists (if it exists, it is the last one in <see cref="Scripts"/>).
        /// </summary>
        public bool HasTheNoVersionScript
        {
            get { return Scripts.Count > 0 && Scripts[Scripts.Count - 1].Script.Name.Version == null; }
        }

        /// <summary>
        /// A (last) versioned script and an optional NoVersion script found.
        /// </summary>
        internal ScriptVector( ISetupScript maxScript, ISetupScript noVersion )
        {
            Debug.Assert( maxScript != null );
            if( noVersion != null ) Scripts = new[] { new CoveringScript( maxScript ), new CoveringScript( noVersion ) };
            else Scripts = new[] { new CoveringScript( maxScript ) };
            Final = maxScript.Name.Version;
        }

        /// <summary>
        /// A starting script, a migration script and an optional NoVersion one.
        /// </summary>
        internal ScriptVector( ISetupScript startingScript, ISetupScript migrationScript, ISetupScript noVersion )
        {
            Debug.Assert( startingScript != null && migrationScript != null );

            var a = noVersion != null
                        ? new[] { new CoveringScript( startingScript ), new CoveringScript( migrationScript ), new CoveringScript( noVersion ) }
                        : new[] { new CoveringScript( startingScript ), new CoveringScript( migrationScript ) };

            Scripts = a;
            Final = migrationScript.Name.Version;
        }

        /// <summary>
        /// Only the NoVersion script found. No script with version at all.
        /// </summary>
        internal ScriptVector( ISetupScript noVersion )
            : this( noVersion, null )
        {
        }

        /// <summary>
        /// A list of versioned script and an optional NoVersion script found.
        /// </summary>
        internal ScriptVector( List<CoveringScript> scripts, ISetupScript noVersion )
        {
            Final = scripts[scripts.Count - 1].Script.Name.Version;
            if( noVersion != null ) scripts.Add( new CoveringScript( noVersion ) );
            Scripts = scripts.ToArray();
        }
    }
}
