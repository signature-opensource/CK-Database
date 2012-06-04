using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class PackageTypedScriptVector
    {
        public readonly Version Final;
        
        public readonly IReadOnlyList<CoveringScript> Scripts;
        
        public bool HasTheNoVersionScript
        {
            get { return Scripts.Count > 0 && Scripts[Scripts.Count - 1].Script.Name.Version == null; }
        }

        internal PackageTypedScriptVector()
        {
            Scripts = ReadOnlyListEmpty<CoveringScript>.Empty;
            Final = null;
        }

        internal PackageTypedScriptVector( ISetupScript maxScript, ISetupScript noVersion )
        {
            Debug.Assert( maxScript != null );
            Debug.Assert( noVersion == null || (noVersion.ScriptType == maxScript.ScriptType) );
            if( noVersion != null ) Scripts = new ReadOnlyListOnIList<CoveringScript>( new[] { new CoveringScript( maxScript ), new CoveringScript( noVersion ) } );
            else Scripts = new ReadOnlyListMono<CoveringScript>( new CoveringScript( maxScript ) );
            Final = maxScript.Name.Version;
        }

        internal PackageTypedScriptVector( List<CoveringScript> scripts, ISetupScript noVersion )
        {
            Debug.Assert( scripts.All( s => s.Script.ScriptType == scripts[0].Script.ScriptType ) );
            Final = scripts[scripts.Count - 1].Script.Name.Version;
            if( noVersion != null ) scripts.Add( new CoveringScript( noVersion ) );
            Scripts = scripts.ToReadOnlyList();
        }
    }
}
