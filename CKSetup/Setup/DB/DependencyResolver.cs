using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    public class DependencyResolver
    {
        readonly DependencyEngine _engine; 
        ComponentDB _db;

        internal DependencyResolver( ComponentDB db, TargetRuntime t, IEnumerable<ComponentDependency> roots )
        {
            _db = db;
            _engine = new DependencyEngine( db, t );
            TargetRuntime = t;
            Roots = roots.ToArray();
        }

        public bool IsEmpty => Roots.Count == 0;

        public TargetRuntime TargetRuntime { get; }

        public IReadOnlyList<ComponentDependency> Roots { get; }

        /// <summary>
        /// Runs this resolver.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="remote">
        /// Optional remote interface that allows download of missing components.
        /// </param>
        /// <returns>
        /// On success, the potentially updated component database and resolved dependencies or a default (null,null) pair on error.
        /// </returns>
        public KeyValuePair<ComponentDB,IReadOnlyList<Component>> Run( IActivityMonitor monitor, IComponentDBRemote remote )
        {
            var error = new KeyValuePair<ComponentDB, IReadOnlyList<Component>>();

            using( monitor.OpenInfo().Send( "Initializing root dependencies." ) )
            {
                if( !_engine.Initialize( monitor, Roots )
                    || !UpdateDBIfNeeded( monitor, remote ) ) return error;
            }
            using( monitor.OpenInfo().Send( "Resolving dependencies." ) )
            {
                while( !_engine.ExpandDependencies( monitor ) )
                {
                    if( !UpdateDBIfNeeded( monitor, remote ) ) return error;
                }
            }
            return new KeyValuePair<ComponentDB, IReadOnlyList<Component>>( _db, _engine.Resolved );
        }

        bool UpdateDBIfNeeded( IActivityMonitor monitor, IComponentDBRemote remote )
        {
            if( _engine.HasMissing )
            {
                if( remote != null )
                {
                    var newDb = remote.Download( monitor, TargetRuntime, _engine.MissingDependencies, _engine.MissingEmbedded, _db );
                    if( newDb == null ) return false;
                    _db = newDb;
                    return _engine.OnDatabaseUpdated( monitor, newDb );
                }
                if( _engine.MissingEmbedded.Count > 0 )
                {
                    monitor.Error().Send( $"Missing embeddeds: {_engine.MissingEmbedded.Select( d => d.ToString() ).Concatenate() }." );
                }
                if( _engine.MissingDependencies.Count > 0 )
                {
                    monitor.Error().Send( $"Missing required dependencies: {_engine.MissingDependencies.Select( d => d.ToString() ).Concatenate() }." );
                }
                return false;
            }
            return true;
        }
    }
}
