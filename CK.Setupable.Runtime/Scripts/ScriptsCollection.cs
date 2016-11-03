using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Collection of <see cref="ISetupScript"/> that applies to one setup object and of one "type".
    /// Contained scripts can be enumerated, but the actual usage is to rely on <see cref="GetScriptVector"/> 
    /// method to obtain an ordered set of scripts that should be executed for a 
    /// given <see cref="SetupCallGroupStep">setup step</see> from a starting version to a final version.
    /// </summary>
    public class ScriptsCollection : IReadOnlyCollection<ISetupScript>
    {
        readonly Dictionary<ISetupScript, ISetupScript> _scripts;

        class CompareScript : IEqualityComparer<ISetupScript>
        {
            public bool Equals( ISetupScript xs, ISetupScript ys )
            {
                Debug.Assert( xs.Name.FullName == ys.Name.FullName, "Internal use only, we are working on the same Container: names match." );
                // ScriptSource is ignored since a ScriptSet "merges" same scripts from
                // different sources.
                var x = xs.Name;
                var y = ys.Name;
                return x.SetupStep == y.SetupStep
                    && x.FromVersion == y.FromVersion
                    && x.Version == y.Version;
            }

            public int GetHashCode( ISetupScript xs )
            {
                var x = xs.Name;
                return Util.Hash.Combine( Util.Hash.StartValue, x.SetupStep, x.FromVersion, x.Version ).GetHashCode();
            }
        }
        static readonly CompareScript _cmp = new CompareScript();

        public ScriptsCollection()
        {
            _scripts = new Dictionary<ISetupScript, ISetupScript>( _cmp );
        }

        /// <summary>
        /// Adds a script to this collection (by default a script is not added if the
        /// same script already exists).
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="script">The script to add. Can not be null.</param>
        /// <param name="onExisting">
        /// Optional conflict resolver that takes the new <paramref name="script"/> and 
        /// the existing one (in this order) and returns one of them (returning the second -existing- one is
        /// the same as returning null).
        /// </param>
        /// <returns>True if the script has been added, false otherwise.</returns>
        public bool Add( IActivityMonitor monitor, ISetupScript script, Func<ISetupScript, ISetupScript, ISetupScript> onExisting = null )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( script == null ) throw new ArgumentNullException( nameof( script ) );
            ISetupScript existing;
            if( _scripts.TryGetValue( script, out existing ) )
            {
                ISetupScript result = script == existing ? null : onExisting?.Invoke( script, existing );
                if( result == script )
                {
                    _scripts[script] = script;
                    monitor.Info().Send( $"Script '{script.Name.FileName}' in '{script.Name.ExtraPath}' from source '{script.ScriptSource}' replaced script from source '{existing.ScriptSource}'." );
                }
                else if( result == existing || result == null )
                {
                    monitor.Warn().Send( $"Script '{script.Name.FileName}' in '{script.Name.ExtraPath}' from source '{script.ScriptSource}' is already registered (from source '{existing.ScriptSource}'). It is ignored." );
                }
                else
                {
                    monitor.Info().Send( $"Script '{script.Name.FileName}' in '{script.Name.ExtraPath}' from source '{script.ScriptSource}' and already registered '{existing.Name.FileName}' in '{existing.Name.ExtraPath}' from source '{existing.ScriptSource}' have given birth to a merged script." );
                }
                return false;
            }
            _scripts.Add( script, script );
            return true;
        }

        /// <summary>
        /// Registers a set of resources (multiple <see cref="ResSetupScript"/>) from a <see cref="ResourceLocator"/>, a full name prefix 
        /// and a script source name.
        /// Use the <paramref name="onExisting"/> optional conflict resolver to change the default behavior that is that the first wins.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="scriptSource">The script source name under which registering the <see cref="ISetupScript"/>.</param>
        /// <param name="resLoc">Resource locator.</param>
        /// <param name="context">Context identifier.</param>
        /// <param name="location">Location identifier.</param>
        /// <param name="name">Name of the object. This is used as a prefix for the resource names.</param>
        /// <param name="fileSuffix">Keeps only resources that ends with this suffix.</param>
        /// <param name="onExisting">
        /// Optional conflict resolver that takes the new <paramref name="script"/> and 
        /// the existing one (in this order) and returns one of them (returning the second -exisiting- one is
        /// the same as returning null).
        /// </param>
        /// <returns>The number of scripts that have been added.</returns>
        public int AddFromResources( 
            IActivityMonitor monitor, 
            string scriptSource, 
            ResourceLocator resLoc, 
            string context, 
            string location, 
            string name, 
            string fileSuffix,
            Func<ISetupScript, ISetupScript, ISetupScript> onExisting = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( scriptSource == null ) throw new ArgumentNullException( "scriptSource" );
            if( resLoc == null ) throw new ArgumentNullException( "scriptSource" );
            if( name == null ) throw new ArgumentNullException( "name" );
            if( fileSuffix == null ) throw new ArgumentNullException( "fileSuffix" );
            int count = 0;
            var candidates = resLoc.GetNames( name + '.' ).Where( n => n.EndsWith( fileSuffix, StringComparison.OrdinalIgnoreCase ) );
            foreach( var s in candidates )
            {
                ParsedFileName rName;
                if( ParsedFileName.TryParse( context, location, s, resLoc, out rName ) )
                {
                    if( Add( monitor, new ResSetupScript( rName, scriptSource ), onExisting ) ) ++count;
                }
            }
            return count;
        }

        /// <summary>
        /// Computes the list of scripts to execute to upgrade from a version to another one.
        /// </summary>
        /// <param name="step">The setup phasis.</param>
        /// <param name="from">The starting version. Can be null.</param>
        /// <param name="to">The final version. When null, the "no version" script, if it exists, is always returned.</param>
        /// <returns>The list of version or null if no scripts must be executed.</returns>
        public ScriptVector GetScriptVector( SetupCallGroupStep step, Version from, Version to )
        {
            Debug.Assert( _scripts.Values.Where( s => s.Name.CallContainerStep == step ).Count( s => s.Name.Version == null ) <= 1, "There is either 0 or 1 'no version' script for a step." );

            if( to == null )
            {
                // Delivers only the NoVersion script if it exists.
                var noV = _scripts.Values.Where( s => s.Name.CallContainerStep == step && s.Name.Version == null ).SingleOrDefault();
                if( noV != null ) return new ScriptVector( noV );
                return null;
            }

            var versionStep = _scripts.Values.Where( s => s.Name.CallContainerStep == step && s.Name.Version != null && !s.Name.IsDowngradeScript && s.Name.Version <= to );
            var noVersion = _scripts.Values.Where( s => s.Name.CallContainerStep == step ).FirstOrDefault( s => s.Name.Version == null );
            if( from == null )
            {
                // If there is no "from", consider the best one as the starting point.
                // If there is no script at all, there is nothing to do.
                if( !versionStep.Any() ) return noVersion != null ? new ScriptVector( noVersion ) : null;
                // Looking for the best version script, not migration one.
                var startingVersions = versionStep.Where( s => s.Name.FromVersion == null );
                // If there is only migration scripts... there is nothing to do.
                if( !startingVersions.Any() ) return noVersion != null ? new ScriptVector( noVersion ) : null;
                // Taking the better one.
                ISetupScript maxVersion = startingVersions.MaxBy( s => s.Name.Version );

                var fromScripts = versionStep.Where( s => s.Name.BelongsToUpgradeFrom( maxVersion.Name.Version ) ).ToList();
                if( fromScripts.Count == 0 ) return new ScriptVector( maxVersion, noVersion );
                if( fromScripts.Count == 1 ) return new ScriptVector( maxVersion, fromScripts[0], noVersion );

                fromScripts.Sort( CoveringScript.CompareUpgradeScripts );
                List<CoveringScript> coveringMigrationScripts = CoveringScript.BuildCoveringScripts( fromScripts );
                coveringMigrationScripts.Insert( 0, new CoveringScript( maxVersion ) );
                return new ScriptVector( coveringMigrationScripts, noVersion );
            }
            var scripts = versionStep.Where( s => s.Name.BelongsToUpgradeFrom( from ) ).ToList();
            if( scripts.Count == 0 ) return noVersion != null ? new ScriptVector( noVersion ) : null;
            if( scripts.Count == 1 ) return new ScriptVector( scripts[0], noVersion );

            scripts.Sort( CoveringScript.CompareUpgradeScripts );
            List<CoveringScript> coveringScripts = CoveringScript.BuildCoveringScripts( scripts );
            return new ScriptVector( coveringScripts, noVersion );
        }

        public IEnumerator<ISetupScript> GetEnumerator() => _scripts.Values.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets the number of scripts this collection contains.
        /// </summary>
        public int Count => _scripts.Count;

    }
}
