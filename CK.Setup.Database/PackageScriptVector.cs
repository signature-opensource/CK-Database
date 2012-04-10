using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup.Database
{
    public class PackageScriptVector
    {
        public readonly Version Final;
        
        public readonly IReadOnlyList<ISetupScript> Scripts;
        
        public bool HasTheNoVersionScript
        {
            get { return Scripts.Count > 0 && Scripts[Scripts.Count - 1].Name.Version == null; }
        }

        internal PackageScriptVector()
        {
            Scripts = ReadOnlyListEmpty<ISetupScript>.Empty;
            Final = null;
        }

        internal PackageScriptVector( ISetupScript maxScript, ISetupScript noVersion )
        {
            Debug.Assert( maxScript != null );
            if( noVersion != null ) Scripts = new ReadOnlyListOnIList<ISetupScript>( new[] { maxScript, noVersion });
            else Scripts = new ReadOnlyListMono<ISetupScript>( maxScript );
            Final = maxScript.Name.Version;
        }

        internal PackageScriptVector( IList<ISetupScript> scripts, ISetupScript noVersion )
        {
            Final = scripts[scripts.Count - 1].Name.Version;
            if( noVersion != null ) scripts.Add( noVersion );
            Scripts = scripts.ToReadOnlyList();
        }
    }
}
