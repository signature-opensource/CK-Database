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

        /// <summary>
        /// Registers types discovered by an <see cref="AssemblyDiscoverer"/>.
        /// </summary>
        /// <param name="discoverer">The discoverer that contains types.</param>
        /// <param name="logger">Logger to use. Can not be null.</param>
        /// <returns>The number of new discovered classes.</returns>
        public int RegisterTypes( AssemblyDiscoverer discoverer, IActivityLogger logger )
        {
            if( discoverer == null ) throw new ArgumentNullException( "discoverer" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
            int totalRegistered = 0;
            using( logger.OpenGroup( LogLevel.Trace, "Registering {0} assemblies.", discoverer.Assemblies.Count ) )
            {
                foreach( var one in discoverer.Assemblies )
                {
                    using( logger.OpenGroup( LogLevel.Trace, "Registering assembly '{0}'.", one.Assembly.FullName ) )
                    {
                        int nbAlready = _cc.RegisteredTypeCount;
                        _cc.Register( one.Types );
                        int delta = _cc.RegisteredTypeCount - nbAlready;
                        logger.CloseGroup( String.Format( "{0} types(s) registered.", delta ) );
                        totalRegistered += delta;
                    }
                }
            }
            return totalRegistered;
        }

        /// <summary>
        /// Registers a class.
        /// </summary>
        /// <param name="c">Class to register.</param>
        /// <returns>True if it is a new class for this collector, false if it has already been registered.</returns>
        public bool RegisterClass( Type c )
        {
            return _cc.RegisterClass( c );
        }

        /// <summary>
        /// Builds and returns a <see cref="StObjCollectorResult"/>.
        /// </summary>
        /// <param name="logger">Logger to use. Can not be null.</param>
        /// <returns>The result.</returns>
        public StObjCollectorResult GetResult( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );

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
                    using( logger.OpenGroup( LogLevel.Info, "Working on Context '{0}'.", r.Context == AmbiantContractCollector.DefaultContext ? "(default)" : r.Context.Name ) )
                    {
                        CreateMutableItems( logger, r );
                        logger.CloseGroup( String.Format( " {0} items created for {1} types.", r.MutableItems.Count, r.AmbiantContractResult.ConcreteClasses.Count ) );
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

            using( logger.Catch( e => result.SetFatal() ) )
            using( logger.OpenGroup( LogLevel.Info, "Initializing object graph." ) )
            {
                foreach( ISortedItem sorted in sortResult.SortedItems )
                {
                    var m = (MutableItem)sorted.Item;
                    if( !m.IsContainer || sorted.IsContainerHead )
                    {
                        using( logger.OpenGroup( LogLevel.Trace, "Initializing '{0}'.", m.ToString() ) )
                        {
                            try
                            {
                                m.CallConstruct( logger, _dependencyResolver );
                            }
                            catch( Exception ex )
                            {
                                logger.Error( ex );
                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert( m.IsContainer && !sorted.IsContainerHead );
                        // We may call here a ConstructContent( IReadOnlyList<IStObj> packageContent ).
                        // But, is it a good thing for a package object to know its content detail ?
                    }
                }
                return result;
            }
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
                MutableItem parent = null;
                foreach( var t in pathTypes )
                {
                    var m = new MutableItem( parent, r.Context, t, theObject );
                    m.ApplyAttributes( logger );
                    r.Add( m );
                    if( _configurator != null ) _configurator.Configure( m );
                    parent = m;
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
