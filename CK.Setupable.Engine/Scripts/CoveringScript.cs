using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Represents a <see cref="Script"/> that covers one or more other scripts: the script has a
    /// final <see cref="ParsedFileName.Version">Version</see> that is greater than any <see cref="ParsedFileName.FromVersion"/> of its <see cref="CoveredScripts"/>
    /// and a FromVersion that is less than or equal to any FromVersion  of its CoveredScripts.
    /// </summary>
    public class CoveringScript
    {
        /// <summary>
        /// Gets the actual <see cref="ISetupScript"/>.
        /// </summary>
        public ISetupScript Script { get; private set; }

        /// <summary>
        /// Gets scripts that are covered by this script if any.
        /// </summary>
        public IReadOnlyList<CoveringScript> CoveredScripts { get; private set; }

        internal CoveringScript( ISetupScript s )
        {
            Debug.Assert( s != null );
            Script = s;
        }

        internal CoveringScript( ISetupScript s, List<ISetupScript> covered )
        {
            Debug.Assert( s != null );
            Script = s;
            if( covered != null )
            {
                Debug.Assert( covered.Count > 0 && covered.IsSortedStrict( CompareUpgradeScripts ) );
                Debug.Assert( covered.All( c => s.Name.FromVersion <= c.Name.FromVersion ), "The covering script starts before any covered script." );
                Debug.Assert( covered.All( c => s.Name.Version > c.Name.FromVersion ), "The covering script brings the system to a version strictely greater than the starting point of any covered script." );
                CoveredScripts = BuildCoveringScripts( covered ).ToReadOnlyList();
            }
            else CoveredScripts = ReadOnlyListEmpty<CoveringScript>.Empty;
        }

        static internal List<CoveringScript> BuildCoveringScripts( List<ISetupScript> scripts )
        {
            List<CoveringScript> coveringScripts = new List<CoveringScript>();
            int i = 0;
            while( i < scripts.Count - 1 )
            {
                ISetupScript current = scripts[i];
                ISetupScript next = scripts[++i];
                List<ISetupScript> coverCovering = null;
                if( current.Name.Version > next.Name.FromVersion )
                {
                    coverCovering = new List<ISetupScript>();
                    do
                    {
                        coverCovering.Add( next );
                        if( ++i == scripts.Count ) break;
                        next = scripts[i];
                    }
                    while( current.Name.Version > next.Name.FromVersion );
                }
                CoveringScript cover = new CoveringScript( current, coverCovering );
                coveringScripts.Add( cover );
            }
            if( i == scripts.Count - 1 ) coveringScripts.Add( new CoveringScript( scripts[i] ) );
            return coveringScripts;
        }

        static internal int CompareUpgradeScripts( ISetupScript x, ISetupScript y )
        {
            int cmp = x.Name.FromVersion.CompareTo( y.Name.FromVersion );
            // Invert the comparison here: privilegiate y. 
            if( cmp == 0 ) cmp = y.Name.Version.CompareTo( x.Name.Version );
            return cmp;
        }

    }

}
