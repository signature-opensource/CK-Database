#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\TypedScriptVector.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
    public class TypedScriptVector
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
        internal TypedScriptVector( ISetupScript maxScript, ISetupScript noVersion )
        {
            Debug.Assert( maxScript != null );
            Debug.Assert( noVersion == null || (noVersion.ScriptSource == maxScript.ScriptSource) );
            if( noVersion != null ) Scripts = new CKReadOnlyListOnIList<CoveringScript>( new[] { new CoveringScript( maxScript ), new CoveringScript( noVersion ) } );
            else Scripts = new CKReadOnlyListMono<CoveringScript>( new CoveringScript( maxScript ) );
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

            Scripts = new CKReadOnlyListOnIList<CoveringScript>( a );
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
