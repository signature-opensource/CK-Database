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
    /// Once Types are registered (<see cref="RegisterTypes"/> and <see cref="RegisterClass"/>), the <see cref="GetResult"/> method
    /// initializes the full object graph.
    /// </summary>
    public class StObjCollector
    {
        readonly AmbientContractCollector<StObjTypeInfo> _cc;
        readonly IStObjStructuralConfigurator _configurator;
        readonly IStObjValueResolver _valueResolver;
        readonly IActivityLogger _logger;
        int _registerFatalOrErrorCount;

        /// <summary>
        /// Initializes a new <see cref="StObjCollector"/>.
        /// </summary>
        /// <param name="logger">Logger to use. Can not be null.</param>
        /// <param name="dispatcher"></param>
        /// <param name="configurator"></param>
        /// <param name="valueResolver"></param>
        public StObjCollector( IActivityLogger logger, IAmbientContractDispatcher dispatcher = null, IStObjStructuralConfigurator configurator = null, IStObjValueResolver dependencyResolver = null )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            _logger = logger;
            _cc = new AmbientContractCollector<StObjTypeInfo>( _logger, ( l, p, t ) => new StObjTypeInfo( l, p, t ), dispatcher );
            _configurator = configurator;
            _valueResolver = dependencyResolver;
        }

        /// <summary>
        /// Gets the count of error or fatal that occured during <see cref="RegisterTypes"/> or <see cref="RegisterClass"/> calls.
        /// </summary>
        public int RegisteringFatalOrErrorCount
        {
            get { return _registerFatalOrErrorCount; }
        }

        /// <summary>
        /// Sets <see cref="RegisteringFatalOrErrorCount"/> to 0.
        /// </summary>
        public void ClearRegisteringErrors()
        {
            _registerFatalOrErrorCount = 0;
        }

        /// <summary>
        /// Registers types discovered by an <see cref="AssemblyRegisterer"/>.
        /// </summary>
        /// <param name="registerer">The discoverer that contains assemblies/types.</param>
        /// <returns>The number of new discovered classes.</returns>
        public int RegisterTypes( AssemblyRegisterer registerer )
        {
            if( registerer == null ) throw new ArgumentNullException( "registerer" );
            int totalRegistered = 0;
            using( _logger.CatchCounter( count => _registerFatalOrErrorCount += count, true ) )
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
            using( _logger.CatchCounter( count => _registerFatalOrErrorCount += count, true ) )
            {
                return _cc.RegisterClass( c );
            }
        }

        /// <summary>
        /// Gets or sets a function that will be called with the list of items once all of them are registered.
        /// </summary>
        public Action<IEnumerable<IDependentItem>> DependencySorterHookInput { get; set; }

        /// <summary>
        /// Gets or sets a function that will be called when items have been successfuly sorted.
        /// </summary>
        public Action<IEnumerable<ISortedItem>> DependencySorterHookOutput { get; set; }

        /// <summary>
        /// Builds and returns a <see cref="StObjCollectorResult"/> if no error occured during type registration.
        /// If <see cref="RegisteringFatalOrErrorCount"/> is not equal to 0, this throws a <see cref="CKException"/>.
        /// To ignore registering errors, calls <see cref="ClearRegisteringErrors"/> before calling this method.
        /// </summary>
        /// <returns>The result.</returns>
        public StObjCollectorResult GetResult()
        {
            if( _registerFatalOrErrorCount > 0 )
            {
                throw new CKException( "There are {0} registration errors. ClearRegisteringErrors must be called before calling this GetResult method to ignore regstration errors.", _registerFatalOrErrorCount );
            }
            AmbientContractCollectorResult<StObjTypeInfo> contracts;
            using( _logger.OpenGroup( LogLevel.Info, "Collecting Ambient Contracts and Type structure." ) )
            {
                contracts = _cc.GetResult();
                contracts.LogErrorAndWarnings( _logger );
            }
            var stObjMapper = new StObjMapper();
            var result = new StObjCollectorResult( stObjMapper, contracts );
            if( result.HasFatalError ) return result;

            using( _logger.OpenGroup( LogLevel.Info, "Creating Structure Objects." ) )
            {
                int objectCount = 0;
                foreach( StObjCollectorContextualResult r in result.Contexts )
                {
                    using( _logger.Catch( e => r.SetFatal() ) )
                    using( _logger.OpenGroup( LogLevel.Info, "Working on Context [{0}].", r.Context ) )
                    {
                        CreateMutableItems( r );
                        _logger.CloseGroup( String.Format( " {0} items created for {1} types.", r.MutableItems.Count, r.AmbientContractResult.ConcreteClasses.Count ) );
                        objectCount += r.MutableItems.Count;
                    }
                }
                if( result.HasFatalError ) return result;
                _logger.CloseGroup( String.Format( "{0} items created.", objectCount ) );
            }

            DependencySorterResult sortResult = null;
            using( _logger.OpenGroup( LogLevel.Info, "Handling dependencies." ) )
            {
                bool noCycleDetected;
                if( !PrepareDependentItems( result, out noCycleDetected ) )
                {
                    _logger.CloseGroup( "Prepare failed." );
                    Debug.Assert( result.HasFatalError );
                    return result;
                }
                if( !ResolveAmbientProperties( result ) )
                {
                    _logger.CloseGroup( "Resolving Ambient Properties failed." );
                    Debug.Assert( result.HasFatalError );
                    return result;
                }
                sortResult = DependencySorter.OrderItems( result.AllMutableItems, null, new DependencySorter.Options() 
                                                                                                { 
                                                                                                    SkipDependencyToContainer = true, 
                                                                                                    HookInput = DependencySorterHookInput, 
                                                                                                    HookOutput = DependencySorterHookOutput 
                                                                                                } );
                Debug.Assert( sortResult.HasRequiredMissing == false, 
                    "A missing requirement can not exist at this stage since we only inject existing Mutable items: missing unresolved dependencies are handled by PrepareDependentItems that logs Errors when needed." );
                Debug.Assert( noCycleDetected || (sortResult.CycleDetected != null), "Cycle detected during item preparation => Cycle detected by the DependencySorter." );
                if( !sortResult.IsComplete )
                {
                    sortResult.LogError( _logger );
                    _logger.CloseGroup( "Ordering failed." );
                    result.SetFatal();
                    return result;
                }
            }
            Debug.Assert( sortResult != null );

            // The structure objects have been ordered by their dependencies (and optionally
            // by the IStObjStructuralConfigurator). 
            // Their instance has been set during the first step (CreateMutableItems).
            // We can now call the Construct methods and returns an ordered list of IStObj.
            //
            using( _logger.Catch( e => result.SetFatal() ) )
            using( _logger.OpenGroup( LogLevel.Info, "Initializing object graph." ) )
            {
                List<IStObj> ordered = new List<IStObj>();
                foreach( ISortedItem sorted in sortResult.SortedItems )
                {
                    var m = (MutableItem)sorted.Item;
                    // Calls Construct on Head for Groups.
                    if( m.ItemKind == DependentItemKind.Item || sorted.IsGroupHead )
                    {
                        m.SetSorterData( ordered.Count, sorted.Requires, sorted.Children, sorted.Groups );
                        using( _logger.OpenGroup( LogLevel.Trace, "Constructing '{0}'.", m.ToString() ) )
                        {
                            try
                            {
                                m.CallConstruct( _logger, _valueResolver );
                            }
                            catch( Exception ex )
                            {
                                _logger.Error( ex );
                            }
                        }
                        ordered.Add( m );
                    }
                    else
                    {
                        Debug.Assert( m.ItemKind != DependentItemKind.Item && !sorted.IsGroupHead );
                        // We may call here a ConstructContent( IReadOnlyList<IStObj> packageContent ).
                        // But... is it a good thing for a package object to know its content detail?
                    }
                }
                using( _logger.OpenGroup( LogLevel.Info, "Setting Ambient Contracts." ) )
                {
                    SetAmbientContracts( result );
                }
                if( !result.HasFatalError ) result.SetSuccess( ordered.ToReadOnlyList() );
                return result;
            }
        }


        /// <summary>
        /// Creates one or more StObjMutableItem for each ambient Type, each of them bound 
        /// to an instance created through its default constructor.
        /// This is the very first step.
        /// </summary>
        void CreateMutableItems( StObjCollectorContextualResult r )
        {
            IReadOnlyList<IReadOnlyList<StObjTypeInfo>> concreteClasses = r.AmbientContractResult.ConcreteClasses;

            for( int i = concreteClasses.Count-1; i >= 0; --i )
            {
                IReadOnlyList<StObjTypeInfo> pathTypes = concreteClasses[i];
                Debug.Assert( pathTypes.Count > 0, "At least the final concrete class exists." );

                // We create items from bottom to top in order for specialization specific 
                // data (like AllAmbientProperties) to be initalized during this creation pass.
                object theObject = pathTypes[pathTypes.Count - 1].CreateInstance( _logger );
                // If we failed to create an instance, we ensure that an error is logged and
                // continue the process.
                if( theObject == null )
                {
                    _logger.Error( "Unable to create an instance of '{0}'.", pathTypes[pathTypes.Count - 1].Type.FullName );
                    continue;
                }
                MutableItem specialization = null;
                MutableItem m = null;
                for( int iT = pathTypes.Count-1; iT >= 0; --iT )
                {
                    m = new MutableItem( m, r.Context, pathTypes[iT], theObject );
                    if( specialization == null ) specialization = r._specializations[i] = m;
                }
                MutableItem generalization = m;
                // Finalize configuration by sollicitating IStObjStructuralConfigurator.
                // It is important here to go top-down since specialized configuration 
                // should override more general ones.
                // Note that this works because we do NOT offer any access to Specialization 
                // in IStObjMutableItem. We actually could offer an access to the Generalization 
                // since it is configured, but it seems useless and may block us later.
                Debug.Assert( typeof( IStObjMutableItem ).GetProperty( "Generalization" ) == null );
                Debug.Assert( typeof( IStObjMutableItem ).GetProperty( "Specialization" ) == null );
                m = generalization;
                do
                {
                    m.ConfigureTopDown( _logger, generalization, specialization );
                    if( _configurator != null ) _configurator.Configure( _logger, m );
                    r.AddStObjConfiguredItem( m );
                }
                while( (m = m.Specialization) != null );
            }
        }

        /// <summary>
        /// Transfers construct parameters type as requirements for the object, binds dependent types to their respective MutableItem,
        /// resolve generalization and container inheritance, and intializes StObjProperties.
        /// </summary>
        bool PrepareDependentItems( StObjCollectorResult collector, out bool noCycleDetected )
        {
            noCycleDetected = true;
            foreach( StObjCollectorContextualResult contextResult in collector.Contexts )
            {
                using( _logger.Catch( e => contextResult.SetFatal() ) )
                using( _logger.OpenGroup( LogLevel.Info, "Working on Context [{0}].", contextResult.Context ) )
                {
                    foreach( MutableItem item in contextResult._specializations )
                    {
                        noCycleDetected &= item.PrepareDependendtItem( _logger, _valueResolver, collector, contextResult );
                    }
                }
            }
            return !collector.HasFatalError;
        }

        /// <summary>
        /// This is the last step: all mutable items have now been created and configured, they are ready to be sorted.
        /// </summary>
        bool ResolveAmbientProperties( StObjCollectorResult collector )
        {
            foreach( StObjCollectorContextualResult contextResult in collector.Contexts )
            {
                using( _logger.Catch( e => contextResult.SetFatal() ) )
                using( _logger.OpenGroup( LogLevel.Info, "Working on Context [{0}].", contextResult.Context ) )
                {
                    foreach( MutableItem item in contextResult._specializations )
                    {
                        item.SetDirectAndResolveAmbientPropertiesOnSpecialization( _logger, collector, _valueResolver );
                    }
                }
            }
            return !collector.HasFatalError;
        }

        /// <summary>
        /// Finalize construction by injecting Ambient Contracts objects on specializations.
        /// </summary>
        bool SetAmbientContracts( StObjCollectorResult collector )
        {
            foreach( StObjCollectorContextualResult contextResult in collector.Contexts )
            {
                using( _logger.Catch( e => contextResult.SetFatal() ) )
                using( _logger.OpenGroup( LogLevel.Info, "Working on Context [{0}].", contextResult.Context ) )
                {
                    foreach( MutableItem item in contextResult._specializations )
                    {
                        item.SetAmbientContracts( _logger, collector, contextResult );
                    }
                }
            }
            return !collector.HasFatalError;
        }

    }

}
