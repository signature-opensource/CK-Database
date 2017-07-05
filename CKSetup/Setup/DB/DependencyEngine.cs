using CK.Core;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    class DependencyEngine
    {
        readonly TargetRuntime _target;
        // This contains fixed requirements of embedded ComponentRef.
        readonly HashSet<ComponentRef> _embeddeds;
        readonly List<ComponentDependency> _deps;
        readonly List<Component> _resolved;

        ComponentDB _db;
        
        public DependencyEngine( ComponentDB db, TargetRuntime t )
        {
            _db = db;
            _target = t;
            _resolved = new List<Component>();
            _deps = new List<ComponentDependency>();
            _embeddeds = new HashSet<ComponentRef>();
        }

        /// <summary>
        /// Gets resolved dependencies.
        /// </summary>
        public List<Component> Resolved => _resolved;

        /// <summary>
        /// Gets the smallest index of <see cref="Resolved"/> dependencies that has
        /// been upgraded to a greater version.
        /// </summary>
        public int ResolvedUpgradeMinIndex { get; private set; }

        /// <summary>
        /// Gets any missing dependencies that have been found.
        /// </summary>
        public IReadOnlyCollection<ComponentDependency> MissingDependencies => _deps;

        /// <summary>
        /// Gets any missing embedded references that have been found.
        /// </summary>
        public IReadOnlyCollection<ComponentRef> MissingEmbedded => _embeddeds;

        /// <summary>
        /// Gets whether a missing dependency or embedded has been found.
        /// </summary>
        public bool HasMissing => _embeddeds.Count > 0 || _deps.Count > 0;

        /// <summary>
        /// Gets the root dependencies.
        /// </summary>
        public IReadOnlyList<ComponentDependency> Roots { get; private set; }

        /// <summary>
        /// Initializes this engine with initial dependencies.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="roots">The intitial required dependencies.</param>
        /// <returns>False if <see cref="HasConflict"/>, true otherwise.</returns>
        internal bool Initialize( IActivityMonitor monitor, IReadOnlyList<ComponentDependency> roots )
        {
            Roots = roots;
            foreach( var d in roots ) Handle( monitor, d );
            return true;
        }

        public bool ExpandDependencies( IActivityMonitor monitor )
        {
            int idx = 0;
            do
            {
                foreach( var dep in _resolved[idx].Dependencies )
                {
                    int idxLocalUgraded = Handle( monitor, dep, _resolved[idx].Name );
                    if( HasMissing ) return false;
                    if( idxLocalUgraded >= 0 && idxLocalUgraded <= idx )
                    {
                        // An already existing resolved dependency has been upgraded
                        // and it has been (or is being) processed.
                        // We restart the process, keeping at least the root dependencies.
                        int succesfulIndex = Math.Max( idxLocalUgraded + 1, Roots.Count );
                        int countToRemove = _resolved.Count - succesfulIndex;
                        _resolved.RemoveRange( succesfulIndex, countToRemove );
                        idx = succesfulIndex;
                        monitor.Trace().Send( $"Restarting from {succesfulIndex}, backtracking {countToRemove} previous resoltions." );
                    }
                }
            }
            while( ++idx < _resolved.Count );
            return true;
        }

        public bool OnDatabaseUpdated( IActivityMonitor monitor, ComponentDB db )
        {
            var unified = _resolved.Select( r => new ComponentDependency( r.Name, r.Version ) )
                                .Concat( _deps )
                                .Concat( _embeddeds.Select( d => new ComponentDependency( d.Name, d.Version ) ) )
                                .GroupBy( d => d.UseName )
                                .Select( g => g.MaxBy( d => d.UseMinVersion ) );
            _resolved.Clear();
            _deps.Clear();
            _embeddeds.Clear();
            _resolved.AddRange( unified.Select( d => db.FindBest( _target, d.UseName, d.UseMinVersion ) ) );
            _db = db;
            Debug.Assert( _resolved.Select( r => r.Name ).SequenceEqual( Roots.Select( r => r.UseName ) ) );
            return true;
        }

        /// <summary>
        /// Handles a dependency.
        /// </summary>
        /// <param name="d">The dependency to check.</param>
        int Handle( IActivityMonitor monitor, ComponentDependency d, string sourceName = "" )
        {
            int idxLocalUpgraded = FindLocal( monitor, d );
            if( idxLocalUpgraded == -2 )
            {
                int idx = _deps.FindIndex( x => x.UseName == d.UseName && x.UseMinVersion >= d.UseMinVersion );
                if( idx >= 0 )
                {
                    monitor.Debug().Send( $"Already existing dependency {_deps[idx]} for {d}." );
                }
                else
                {
                    idx = _deps.FindIndex( x => x.UseName == d.UseName && x.UseMinVersion < d.UseMinVersion );
                    if( idx >= 0 )
                    {
                        monitor.Trace().Send( $"Upgrading required dependency {sourceName}{d}." );
                        _deps[idx] = d;
                    }
                    else
                    {
                        monitor.Trace().Send( $"Adding required dependency {sourceName}{d}." );
                        _deps.Add( d );
                    }
                }
            }
            return idxLocalUpgraded;
        }

        int FindLocal( IActivityMonitor monitor, ComponentDependency d )
        {
            int idx = _resolved.FindIndex( c => c.Name == d.UseName );
            if( idx < 0 )
            {
                var found = _db.FindBest( _target, d.UseName, d.UseMinVersion );
                if( found != null )
                {
                    monitor.Trace().Send( $"Resolved required dependency {d} to local {found}." );
                    _resolved.Add( HandleEmbedded( monitor, found ) );
                    return -1;
                }
            }
            else
            {
                var exists = _resolved[idx];
                if( exists.Version >= d.UseMinVersion )
                {
                    monitor.Debug().Send( $"Required dependency {d} resolved to existing local {exists}." );
                    return -1;
                }
                var found = _db.FindBest( _target, d.UseName, d.UseMinVersion );
                if( found != null )
                {
                    monitor.Trace().Send( $"Local {exists} upgraded to local {found}." );
                    _resolved[idx] = HandleEmbedded( monitor, found );
                    return idx;
                }
            }
            return -2;
        }

        Component HandleEmbedded( IActivityMonitor monitor, Component found )
        {
            foreach( var m in found.Embedded )
            {
                if( _embeddeds.Add( m ) )
                {
                    int idx = _deps.FindIndex( x => x.UseName == m.Name && x.UseMinVersion <= m.Version );
                    if( idx >= 0 )
                    {
                        // The exact reference will satisfy the floating one.
                        monitor.Trace().Send( $"Required dependency {_deps[idx]} upgraded to embedded {m}." );
                        _deps.RemoveAt( idx );
                    }
                }
            }
            return found;
        }

    }
}
