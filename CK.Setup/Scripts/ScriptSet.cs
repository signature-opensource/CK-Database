using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Groups <see cref="ISetupScript">scripts</see> by object's <see cref="FullName"/> to which they apply 
    /// and by <see cref="ScriptTypeHandler"/> that are able to manage them (see <see cref="FindScripts"/>).
    /// </summary>
    public class ScriptSet
    {
        readonly List<ForHandler> _handlers;
        readonly IReadOnlyList<ForHandler> _handlersEx;
        readonly string _fullName;

        /// <summary>
        /// Collection of <see cref="ISetupScript"/> that applies to one setup object and of one type (for same script occuring 
        /// in multiple sources, the <see cref="ScriptSource.Index"/> has been used to choose the one to keep).
        /// Contained scripts can be enumerated, but the actual usage is to rely on <see cref="GetScriptVector"/> method to obtain an ordered set
        /// of scripts that should be executed for a given <see cref="SetupCallGroupStep">setup step</see> from a starting version to a final version.
        /// </summary>
        public class ForHandler : IReadOnlyCollection<ISetupScript>
        {
            public readonly ScriptTypeHandler Handler;           
            readonly Dictionary<ISetupScript,ISetupScript> _scripts;

            internal ForHandler( ScriptTypeHandler h )
            {
                Handler = h;
                _scripts = new Dictionary<ISetupScript, ISetupScript>( _cmp );
            }

            internal bool Add( IActivityLogger logger, ScriptSource source, ISetupScript script, ScriptTypeManager manager )
            {
                Debug.Assert( source.Handler == Handler );
                ISetupScript existing;
                if( _scripts.TryGetValue( script, out existing ) )
                {
                    int existingIndex = manager.FindSourceByName( existing.ScriptSource ).Index;
                    if( source.Index > existingIndex )
                    {
                        _scripts[script] = script;
                        logger.Info( "Script '{0}' in '{1}' from source '{2}' has been overriden by source '{3}'.", script.Name.FileName, script.Name.ExtraPath, existing.ScriptSource, source.Name );
                    }
                    else if( source.Index < existingIndex )
                    {
                        // Always Info() as if the operation was done in the other sense: the user is always informed of the override.
                        logger.Info( "Script '{0}' in '{1}' from source '{2}' has been overriden by source '{3}'.", script.Name.FileName, script.Name.ExtraPath, source.Name, existing.ScriptSource );
                    }
                    else
                    {
                        logger.Warn( "Script '{0}' in '{1}' from source '{2}' is already registered. It is ignored.", script.Name.FileName, script.Name.ExtraPath, source.Name );
                    }
                    return false;
                }
                _scripts.Add( script, script );
                return true;
            }

            public TypedScriptVector GetScriptVector( SetupCallGroupStep step, Version from, Version to )
            {
                Debug.Assert( _scripts.Values.Where( s => s.Name.CallContainerStep == step ).Count( s => s.Name.Version == null ) <= 1, "There is either 0 or 1 'no version' script for a step." );

                if( to == null )
                {
                    // Delivers only the NoVersion script if it exists.
                    var noV = _scripts.Values.Where( s => s.Name.CallContainerStep == step && s.Name.Version == null ).SingleOrDefault();
                    if( noV != null ) return new TypedScriptVector( noV );
                    return new TypedScriptVector();
                }

                var versionStep = _scripts.Values.Where( s => s.Name.CallContainerStep == step && s.Name.Version != null && !s.Name.IsDowngradeScript && s.Name.Version <= to );
                var noVersion = _scripts.Values.Where( s => s.Name.CallContainerStep == step ).FirstOrDefault( s => s.Name.Version == null );
                if( from == null )
                {
                    // If there is no "from", consider the best one as the starting point.
                    // If there is no script at all, there is nothing to do.
                    if( !versionStep.Any() ) return new TypedScriptVector();
                    // Looking for the best version script, not migration one.
                    var startingVersions = versionStep.Where( s => s.Name.FromVersion == null );
                    // If there is only migration scripts... there is nothing to do.
                    if( !startingVersions.Any() ) return new TypedScriptVector();
                    // Taking the better one.
                    ISetupScript maxVersion = startingVersions.MaxBy( s => s.Name.Version );
                    
                    var fromScripts = versionStep.Where( s => s.Name.BelongsToUpgradeFrom( maxVersion.Name.Version ) ).ToList();
                    if( fromScripts.Count == 0 ) return new TypedScriptVector( maxVersion, noVersion );
                    if( fromScripts.Count == 1 ) return new TypedScriptVector( maxVersion, fromScripts[0], noVersion );

                    fromScripts.Sort( CoveringScript.CompareUpgradeScripts );
                    List<CoveringScript> coveringMigrationScripts = CoveringScript.BuildCoveringScripts( fromScripts );
                    coveringMigrationScripts.Insert( 0, new CoveringScript( maxVersion ) );
                    return new TypedScriptVector( coveringMigrationScripts, noVersion );
                }
                var scripts = versionStep.Where( s => s.Name.BelongsToUpgradeFrom( from ) ).ToList();
                if( scripts.Count == 0 ) return new TypedScriptVector();
                if( scripts.Count == 1 ) return new TypedScriptVector( scripts[0], noVersion );

                scripts.Sort( CoveringScript.CompareUpgradeScripts );
                List<CoveringScript> coveringScripts = CoveringScript.BuildCoveringScripts( scripts );
                return new TypedScriptVector( coveringScripts, noVersion );
            }

            class CompareScript : IEqualityComparer<ISetupScript>
            {
                public bool Equals( ISetupScript xs, ISetupScript ys )
                {
                    Debug.Assert( xs.Name.FullName == ys.Name.FullName, "Internal use only, we are working on the same Container: names match." );
                    // ScriptSource is ignored since a ScriptSet "merges" same scripts from
                    // different sources (by taking Sources.Index into account).
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
                    return Util.Hash.Combine( Util.Hash.StartValue, x.SetupStep, x.IsContent, x.FromVersion, x.Version ).GetHashCode();
                }
            }
            static readonly CompareScript _cmp = new CompareScript();

            public IEnumerator<ISetupScript> GetEnumerator()
            {
                return _scripts.Values.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            
            public bool Contains( object item )
            {
                ISetupScript s = item as ISetupScript;
                if( s == null ) return false;
                ISetupScript existing;
                return _scripts.TryGetValue( s, out existing ) && existing.ScriptSource == s.ScriptSource;
            }

            public int Count
            {
                get { return _scripts.Count; }
            }

        }

        internal ScriptSet( string fullName )
        {
            _fullName = fullName;
            _handlers = new List<ForHandler>();
            _handlersEx = new ReadOnlyListOnIList<ForHandler>( _handlers );
        }

        /// <summary>
        /// Gets the full name of the item that is associated to these scripts.
        /// </summary>
        public string FullName { get { return _fullName; } }      

        internal bool Add( IActivityLogger logger, ScriptSource source, ISetupScript script, ScriptTypeManager manager )
        {
            Debug.Assert( FullName == script.Name.FullName, String.Format( "Script '{0}' can not be associated to '{1}' (names are case-sensitive).", script.Name.FullName, FullName ) );
            var handler = _handlers.FirstOrDefault( h => h.Handler == source.Handler );
            if( handler == null )
            {
                handler = new ForHandler( source.Handler );
                _handlers.Add( handler );
            }
            return handler.Add( logger, source, script, manager );
        }

        /// <summary>
        /// Gets the scripts grouped by their handler that this set contains.
        /// </summary>
        /// <returns>A collection of <see cref="ForHandler"/> objects.</returns>
        public IReadOnlyCollection<ForHandler> ScriptsByHandlers
        {
            get { return _handlersEx; }
        }

        internal ForHandler FindScripts( ScriptTypeHandler handler )
        {
            return _handlers.FirstOrDefault( f => f.Handler == handler );
        }

    }

}
