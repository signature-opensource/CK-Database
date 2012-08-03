using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;
using System.Reflection;
using System.Collections.Specialized;

namespace CK.Setup
{

    /// <summary>
    /// Collects typed object from assemblies.
    /// </summary>
    public class StObjCollector
    {
        AmbiantContractCollector _cc;
        IEnumerable<StObjHandler> _handlers;

        public StObjCollector( IEnumerable<StObjHandler> handlers )
        {
            if( handlers == null ) throw new ArgumentNullException( "handlers" );
            _handlers = handlers;
            _cc = new AmbiantContractCollector();
        }

        public void RegisterTypes( Assembly a, IActivityLogger logger )
        {
            if( a == null ) throw new ArgumentNullException( "a" );
            if( logger == null ) throw new ArgumentNullException( "logger" );

            using( logger.OpenGroup( LogLevel.Trace, "Registering assembly '{0}'.", a.FullName ) )
            {
                int nbAlready = _cc.RegisteredTypeCount;
                _cc.Register( a.GetTypes(), Util.ActionVoid );
                logger.CloseGroup( String.Format( "{0} types(s) registered.", _cc.RegisteredTypeCount - nbAlready ) );
            }
        }

        class ContextTypeRegisterer : IStObjRegisterer, IDisposable
        {
            IActivityLogger _logger;
            ActivityLoggerSimpleCollector _errorTracker;
            StObjResult _result;
            Dictionary<Type,IDependentItem> _items;

            internal ContextTypeRegisterer( IActivityLogger logger, StObjResult r )
            {
                _logger = logger;
                _result = r;
                _items = new Dictionary<Type, IDependentItem>();
                _errorTracker = new ActivityLoggerSimpleCollector();
                Debug.Assert( _errorTracker.LevelFilter == LogLevelFilter.Error );
                _logger.Output.RegisterClient( _errorTracker );
            }

            Type IStObjRegisterer.Context { get { return _result.Context; } }
            IActivityLogger IStObjRegisterer.Logger { get { return _logger; } }
            void IStObjRegisterer.Register( IStObjDependentItem item ) { _result.Add( item ); }

            internal void Register( IEnumerable<StObjHandler> handlers, IReadOnlyList<Type> pathTypes )
            {
                foreach( var h in handlers )
                {
                    if( h.Register( _logger, pathTypes, this ) ) return;
                }
                _logger.Fatal( "Unable to find a StObjHandler for '{0}'.", String.Join( "' -> '", pathTypes.Select( t => t.FullName ) ) ); 
            }

            public void Dispose()
            {
                _logger.Output.UnregisterClient( _errorTracker );
                if( _errorTracker.Entries.Count > 0 )
                {
                    _result.SetFatal();
                }
                else
                {
                    _result.InitDependentItems( _logger );
                }
            }
        }

        public MultiStObjResult GetResult( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            
            // Step n°0: Collecting Ambiant Contracts.
            MultiAmbiantContractResult contracts = _cc.GetResult();
            contracts.LogErrorAndWarnings( logger );
            
            MultiStObjResult r = new MultiStObjResult( contracts );
            if( r.HasFatalError ) return r;

            using( logger.OpenGroup( LogLevel.Info, "Creating dependent items for typed objects." ) )
            {
                foreach( AmbiantContractResult c in contracts )
                {
                    using( ContextTypeRegisterer reg = new ContextTypeRegisterer( logger, r.Add( new StObjResult( c ) ) ) )
                    {
                        foreach( var pathTypes in c.ConcreteClasses )
                        {
                            Debug.Assert( pathTypes.Count > 0, "At least the final concrete class exists." );
                            reg.Register( _handlers, pathTypes );
                        }
                    }
                }
                if( r.HasFatalError ) return r;
                logger.CloseGroup( String.Format( "{0} items created.", r.TotalDependentItemCount ) );
            }
            return r;
        }

    }

}
