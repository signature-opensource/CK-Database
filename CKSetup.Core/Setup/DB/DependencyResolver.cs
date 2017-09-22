using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        /// <param name="downloader">Optional downloader of missing components.</param>
        /// <returns>Resolved dependencies on success, null on error.</returns>
        public IReadOnlyList<Component> Run( IActivityMonitor monitor, IComponentDownloader downloader )
        {
            using( monitor.OpenInfo( "Initializing root dependencies." ) )
            {
                if( !_engine.Initialize( monitor, Roots )
                    || !UpdateDBIfNeeded( monitor, downloader ) ) return null;
            }
            using( monitor.OpenInfo( "Resolving dependencies." ) )
            {
                while( !_engine.ExpandDependencies( monitor ) )
                {
                    if( !UpdateDBIfNeeded( monitor, downloader ) ) return null;
                }
            }
            return _engine.Resolved;
        }

        bool UpdateDBIfNeeded( IActivityMonitor monitor, IComponentDownloader downloader )
        {
            if( _engine.HasMissing )
            {
                if( downloader != null )
                {
                    using( monitor.OpenInfo( $"Using donwloader." ) )
                    {
                        try
                        {
                            var missing = new ComponentMissingDescription( TargetRuntime, _engine.MissingDependencies, _engine.MissingEmbedded );
                            monitor.Debug( missing.ToXml().ToString() );
                            var newDb = downloader.Download( monitor, missing );
                            if( newDb == null ) return false;
                            if( _db != newDb )
                            {
                                _db = newDb;
                                return _engine.OnDatabaseUpdated( monitor, newDb );
                            }
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( ex );
                            return false;
                        }
                    }
                }
                if( _engine.MissingEmbedded.Count > 0 )
                {
                    monitor.Error( $"Missing embeddeds: {_engine.MissingEmbedded.Select( d => d.ToString() ).Concatenate() }." );
                }
                if( _engine.MissingDependencies.Count > 0 )
                {
                    monitor.Error( $"Missing required dependencies: {_engine.MissingDependencies.Select( d => d.ToString() ).Concatenate() }." );
                }
                return false;
            }
            return true;
        }
    }
}
