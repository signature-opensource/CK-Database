using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;
using System.Reflection;
using System.Collections.Specialized;
using System.Collections;

namespace CK.Setup
{

    /// <summary>
    /// Discovers available structure objects and instanciates them.
    /// </summary>
    public class StObjCollector
    {
        AmbiantContractCollector _cc;
        IStObjExternalConfigurator _configurator;
        IStObjDependencyResolver _dependencyResolver;

        public StObjCollector( IStObjExternalConfigurator configurator = null, IStObjDependencyResolver dependencyResolver = null )
        {
            _cc = new AmbiantContractCollector();
            _configurator = configurator;
            _dependencyResolver = dependencyResolver;
        }

        public void RegisterTypes( AssemblyDiscoverer discoverer, IActivityLogger logger )
        {
            if( discoverer == null ) throw new ArgumentNullException( "discoverer" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
            using( logger.OpenGroup( LogLevel.Trace, "Registering {0} assemblies.", discoverer.Assemblies.Count ) )
            {
                foreach( var one in discoverer.Assemblies )
                {
                    using( logger.OpenGroup( LogLevel.Trace, "Registering assembly '{0}'.", one.Assembly.FullName ) )
                    {
                        int nbAlready = _cc.RegisteredTypeCount;
                        _cc.Register( one.Types );
                        logger.CloseGroup( String.Format( "{0} types(s) registered.", _cc.RegisteredTypeCount - nbAlready ) );
                    }
                }
            }
        }

        public StObjCollectorResult GetResult( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );

            // Step n°0: Collecting Ambiant Contracts.
            AmbiantContractCollectorResult contracts = _cc.GetResult();
            contracts.LogErrorAndWarnings( logger );

            var stObjMapper = new StObjMapper();
            var result = new StObjCollectorResult( stObjMapper, contracts );
            if( result.HasFatalError ) return result;

            using( logger.OpenGroup( LogLevel.Info, "Creating Structure Objects." ) )
            {
                foreach( StObjCollectorContextualResult r in result )
                {
                    using( logger.Catch( e => r.SetFatal() ) )
                    using( r.Context != null ? logger.OpenGroup( LogLevel.Info, "Working on Typed Context '{0}'.", r.Context.Name ) : null )
                    {
                        CreateMutableItems( logger, r );
                        if( r.Context != null ) logger.CloseGroup();
                        logger.CloseGroup( String.Format( " {0} objects created for {1} types.", r.MutableItems.Count, r.AmbiantContractResult.ConcreteClasses.Count ) );
                    }
                }
                if( result.HasFatalError ) return result;
                logger.CloseGroup( String.Format( "{0} items created.", result.TotalItemCount ) );
            }

            DependencySorterResult sortResult = null;
            using( logger.OpenGroup( LogLevel.Info, "Resolving dependencies." ) )
            {
                if( PrepareDependentItems( logger, result ) )
                {
                    sortResult = DependencySorter.OrderItems( result.AllMutableItems, null );
                    Debug.Assert( sortResult.HasRequiredMissing == false, 
                        "A missing requirement can not exist at this stage since we only inject existing Mutable items: missing unresolved dependencies are handled by PrepareDependentItems that logs Errors when needed." );
                    if( !sortResult.IsComplete )
                    {
                        sortResult.LogError( logger );
                        logger.CloseGroup( "Ordering failed." );
                        result.SetFatal();
                    }
                }
            }
            if( result.HasFatalError ) return result;

            Debug.Assert( sortResult != null );
            // The structure objects have been ordered by their dependencies (and optionally
            // by the IStObjExternalConfigurator). 
            // Their instance has been set during the first step (CreateMutableItems).
            // We can now call the Construct methods.
            foreach( ISortedItem sorted in sortResult.SortedItems )
            {
                var m = (MutableItem)sorted.Item;
                m.CallConstruct( logger, result, _dependencyResolver );
            }
            return result;
        }

        /// <summary>
        /// Creates one or more StObjMutableItem for each ambiant Type, each of them bound 
        /// to an instance created through its default constructor.
        /// This is the very first step.
        /// </summary>
        void CreateMutableItems( IActivityLogger logger, StObjCollectorContextualResult r )
        {
            foreach( var pathTypes in r.AmbiantContractResult.ConcreteClasses )
            {
                Debug.Assert( pathTypes.Count > 0, "At least the final concrete class exists." );
                object theObject = Activator.CreateInstance( pathTypes[pathTypes.Count - 1] );
                foreach( var t in pathTypes )
                {
                    var m = new MutableItem( r.Context, t, theObject );
                    m.ApplyAttributes( logger );
                    r.MutableItems.Add( m );
                    if( _configurator != null ) _configurator.Configure( m );
                }
            }
        }

        /// <summary>
        /// Transfers construct parameters type as requirements for the object
        /// and binds dependent types to their respective StObjMutableItem.
        /// This is the second step: all mutable items have now been created and configured.
        /// </summary>
        internal bool PrepareDependentItems( IActivityLogger logger, StObjCollectorResult result )
        {
            foreach( StObjCollectorContextualResult contextResult in result )
            {
                using( logger.Catch( e => contextResult.SetFatal() ) )
                using( contextResult.Context != null ? logger.OpenGroup( LogLevel.Info, "Working on Typed Context '{0}'.", contextResult.Context.Name ) : null )
                {
                    foreach( MutableItem item in contextResult.MutableItems )
                    {
                        item.PrepareDependentItem( logger, result, contextResult );
                    }
                }
            }
            return !result.HasFatalError;
        }

    }

}
