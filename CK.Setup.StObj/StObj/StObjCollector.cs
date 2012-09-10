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
        AmbiantContractCollector<StObjTypeInfo> _cc;
        IStObjStructuralConfigurator _configurator;
        IStObjDependencyResolver _dependencyResolver;
        IActivityLogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger">Logger to use. Can not be null.</param>
        /// <param name="dispatcher"></param>
        /// <param name="configurator"></param>
        /// <param name="dependencyResolver"></param>
        public StObjCollector( IActivityLogger logger, IAmbiantContractDispatcher dispatcher = null, IStObjStructuralConfigurator configurator = null, IStObjDependencyResolver dependencyResolver = null )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            _logger = logger;
            _cc = new AmbiantContractCollector<StObjTypeInfo>( _logger, ( l, p, t ) => new StObjTypeInfo( l, p, t ), dispatcher );
            _configurator = configurator;
            _dependencyResolver = dependencyResolver;
        }

        /// <summary>
        /// Registers types discovered by an <see cref="AssemblyRegisterer"/>.
        /// </summary>
        /// <param name="registerer">The discoverer that contains assemblies/types.</param>
        /// <returns>The number of new discovered classes.</returns>
        public int RegisterTypes( AssemblyRegisterer registerer )
        {
            if( registerer == null ) throw new ArgumentNullException( "discoverer" );
            int totalRegistered = 0;
            using( _logger.OpenGroup( LogLevel.Trace, "Registering {0} assemblies.", registerer.Assemblies.Count ) )
            {
                foreach( var one in registerer.Assemblies )
                {
                    using( _logger.OpenGroup( LogLevel.Trace, "Registering assembly '{0}'.", one.Assembly.FullName ) )
                    {
                        int nbAlready = _cc.RegisteredTypeCount;
                        _cc.Register( one.Types );
                        int delta = _cc.RegisteredTypeCount - nbAlready;
                        _logger.CloseGroup( String.Format( "{0} types(s) registered.", delta ) );
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
        /// <returns>The result.</returns>
        public StObjCollectorResult GetResult()
        {
            var contracts = _cc.GetResult();
            contracts.LogErrorAndWarnings( _logger );

            var stObjMapper = new StObjMapper();
            var result = new StObjCollectorResult( stObjMapper, contracts );
            if( result.HasFatalError ) return result;

            using( _logger.OpenGroup( LogLevel.Info, "Creating Structure Objects." ) )
            {
                int objectCount = 0;
                foreach( StObjCollectorContextualResult r in result )
                {
                    using( _logger.Catch( e => r.SetFatal() ) )
                    using( _logger.OpenGroup( LogLevel.Info, "Working on Context '{0}'.", r.Context == AmbiantContractCollector.DefaultContext ? "(default)" : r.Context.Name ) )
                    {
                        CreateMutableItems( r );
                        _logger.CloseGroup( String.Format( " {0} items created for {1} types.", r.MutableItems.Count, r.AmbiantContractResult.ConcreteClasses.Count ) );
                        objectCount += r.MutableItems.Count;
                    }
                }
                if( result.HasFatalError ) return result;
                _logger.CloseGroup( String.Format( "{0} items created.", objectCount ) );
            }

            DependencySorterResult sortResult = null;
            using( _logger.OpenGroup( LogLevel.Info, "Resolving dependencies." ) )
            {
                if( PrepareDependentItems( _logger, result ) )
                {
                    sortResult = DependencySorter.OrderItems( result.AllMutableItems, null, new DependencySorter.Options() { SkipDependencyToContainer = true } );
                    Debug.Assert( sortResult.HasRequiredMissing == false, 
                        "A missing requirement can not exist at this stage since we only inject existing Mutable items: missing unresolved dependencies are handled by PrepareDependentItems that logs Errors when needed." );
                    if( !sortResult.IsComplete )
                    {
                        sortResult.LogError( _logger );
                        _logger.CloseGroup( "Ordering failed." );
                        result.SetFatal();
                    }
                }
            }
            if( result.HasFatalError ) return result;

            Debug.Assert( sortResult != null );

            // The structure objects have been ordered by their dependencies (and optionally
            // by the IStObjStructuralConfigurator). 
            // Their instance has been set during the first step (CreateMutableItems).
            // We can now call the Construct methods.

            using( _logger.Catch( e => result.SetFatal() ) )
            using( _logger.OpenGroup( LogLevel.Info, "Initializing object graph." ) )
            {
                List<IStObj> ordered = new List<IStObj>();
                using( _logger.OpenGroup( LogLevel.Info, "Graph construction." ) )
                {
                    foreach( ISortedItem sorted in sortResult.SortedItems )
                    {
                        var m = (MutableItem)sorted.Item;
                        if( !m.IsContainer || sorted.IsContainerHead )
                        {
                            m.SetSorterData( ordered.Count, sorted.Container, sorted.Requires );
                            ordered.Add( m );
                            using( _logger.OpenGroup( LogLevel.Trace, "Constructing '{0}'.", m.ToString() ) )
                            {
                                try
                                {
                                    m.CallConstruct( _logger, _dependencyResolver );
                                    // Here, if m.Specialization == null, we have intialized
                                    // the leaf of the inheritance chain.
                                    // Can we initialize Ambiant properties here? Not yet!
                                    //
                                    // Ambiant properties are searched only on containers (first) and then base classes (recursively).
                                    // Ambiant properties must be initialized by the leaf (most specialized).
                                    //
                                    // Even if we just initialized the bottom of a chain here,
                                    // there may be containers of these items that have not been 
                                    // initialized (up to their "leaf")...
                                    //
                                    // So we can NOT resolve ambiant properties here since we
                                    // want Containers holding properties to be initialized first.
                                }
                                catch( Exception ex )
                                {
                                    _logger.Error( ex );
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Debug.Assert( m.IsContainer && !sorted.IsContainerHead );
                            // We may call here a ConstructContent( IReadOnlyList<IStObj> packageContent ).
                            // But... is it a good thing for a package object to know its content detail?
                        }
                    }
                }
                using( _logger.OpenGroup( LogLevel.Info, "Ambiant properties initialization." ) )
                {
                    foreach( MutableItem m in ordered )
                    {
                        if( m.Specialization == null )
                        {
                            using( _logger.OpenGroup( LogLevel.Trace, "EnsureAmbiantPropertiesResolved( {0} )", m.ToString() ) )
                            {
                                m.EnsureAmbiantPropertiesResolved( _logger, result, _dependencyResolver );
                            }
                        }
                    }
                }
                if( !result.HasFatalError ) result.SetSuccess( ordered.ToReadOnlyList() );
                return result;
            }
        }

        /// <summary>
        /// Creates one or more StObjMutableItem for each ambiant Type, each of them bound 
        /// to an instance created through its default constructor.
        /// This is the very first step.
        /// </summary>
        void CreateMutableItems( StObjCollectorContextualResult r )
        {
            foreach( var pathTypes in r.AmbiantContractResult.ConcreteClasses )
            {
                Debug.Assert( pathTypes.Count > 0, "At least the final concrete class exists." );
                object theObject = Activator.CreateInstance( pathTypes[pathTypes.Count - 1].Type );
                MutableItem generalization = null;
                MutableItem m = null;
                foreach( var t in pathTypes )
                {
                    m = new MutableItem( m, r.Context, t, theObject );
                    if( generalization == null ) generalization = m;
                }
                MutableItem specialization = m;
                // We configure items from bottom to top. Even if this may seem
                // strange, this is required for AllAmbiantProperties to
                // be set to the Specialization one.
                // Note that this works because we do NOT offer any access to 
                // Generalization nor to Specialization in IStObjMutableItem.
                Debug.Assert( typeof( IStObjMutableItem ).GetProperty( "Generalization" ) == null );
                Debug.Assert( typeof( IStObjMutableItem ).GetProperty( "Specialization" ) == null );
                do
                {
                    m.Configure( _logger, generalization, specialization );
                    if( _configurator != null ) _configurator.Configure( _logger, m );
                    r.AddConfiguredItem( m );
                }
                while( (m = m.Generalization) != null );
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
                        using( logger.OpenGroup( LogLevel.Trace, "PrepareDependentItem( {0} )", item.ToString() ) )
                        {
                            item.PrepareDependentItem( logger, result, contextResult );
                        }
                    }
                }
            }
            return !result.HasFatalError;
        }

    }

}
