using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup.Database
{
    public class PackageScriptSet
    {
        HashSet<ISetupScript> _scripts;

        class CompareScript : IEqualityComparer<ISetupScript>
        {
            public bool Equals( ISetupScript xs, ISetupScript ys )
            {
                Debug.Assert( xs.Name.FullName == ys.Name.FullName, "Internal use only: names match." );
                if( xs.ScriptType != ys.ScriptType ) return false;
                var x = xs.Name;
                var y = ys.Name;
                return x.SetupStep == y.SetupStep 
                    && x.IsContent == y.IsContent 
                    && x.FromVersion == y.FromVersion
                    && x.Version == y.Version;
            }

            public int GetHashCode( ISetupScript xs )
            {
                var x = xs.Name;
                return Util.Hash.Combine( Util.Hash.StartValue, xs.ScriptType, x.SetupStep, x.IsContent, x.FromVersion, x.Version ).GetHashCode();
            }
        }
        static CompareScript _cmp = new CompareScript();

        public PackageScriptSet( string packageFullName )
        {
            PackageFullName = packageFullName;
            _scripts = new HashSet<ISetupScript>( _cmp );
        }

        public string PackageFullName { get; private set; }

        /// <summary>
        /// Adds a script: only a <see cref="PackageScriptCollector"/> should be able to do this: hence 
        /// the internal protection.
        /// </summary>
        /// <param name="script">The setup script.</param>
        /// <returns></returns>
        internal bool Add( ISetupScript script )
        {
            if( PackageFullName != script.Name.FullName )
            {
                throw new ArgumentException( String.Format( "Script '{0}' can not be associated to '{1}' (names are case-sensitive).", script.Name.FullName, PackageFullName ) );
            }
            return _scripts.Add( script );
        }

        public IEnumerable<ISetupScript> ForType( string scriptType )
        {
            return _scripts.Where( s => s.ScriptType == scriptType );
        }

        public PackageScriptVector GetScriptVector( string scriptType, SetupCallContainerStep step, Version from, Version to )
        {
            if( to == null ) throw new ArgumentNullException( "to" );

            Debug.Assert( _scripts.Where( s => s.ScriptType == scriptType ).Where( s => s.Name.CallContainerStep == step ).Count( s => s.Name.Version == null ) <= 1, "There is either 0 or 1 'no version' script for a step." );
            
            var versionStep = _scripts.Where( s => s.ScriptType == scriptType && s.Name.CallContainerStep == step && s.Name.Version != null && !s.Name.IsDowngradeScript && s.Name.Version <= to );
            var noVersion = _scripts.Where( s => s.ScriptType == scriptType && s.Name.CallContainerStep == step ).FirstOrDefault( s => s.Name.Version == null );
            if( from == null )
            {
                if( versionStep.Any() )
                {
                    ISetupScript maxVersion = versionStep.Where( s => s.Name.FromVersion == null ).MaxBy( s => s.Name.Version );
                    return new PackageScriptVector( maxVersion, noVersion );
                }
                return new PackageScriptVector();
            }
            var scripts = versionStep.Where( s => s.Name.BelongsToUpgradeFrom( from ) ).ToList();
            scripts.Sort( CompareUpgradeScripts );
            for( int i = 0; i < scripts.Count-1; )
            {
                ISetupScript current = scripts[i];
                ISetupScript next = scripts[++i];
                if( current.Name.Version > next.Name.FromVersion ) 
                {
                    scripts.RemoveAt( i-- );
                }
            }
            if( scripts.Count == 0 ) return new PackageScriptVector();
            if( scripts.Count == 1 ) return new PackageScriptVector( scripts[0], noVersion );
            return new PackageScriptVector( scripts, noVersion );
        }

        static int CompareUpgradeScripts( ISetupScript x, ISetupScript y )
        {
            int cmp = x.Name.FromVersion.CompareTo( y.Name.FromVersion );
            // Invert the comparison here: privilegiate y. 
            if( cmp == 0 ) cmp = y.Name.Version.CompareTo( x.Name.Version );
            return cmp;
        }
    }
}
