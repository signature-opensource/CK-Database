using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using CK.Core;
using System.Reflection;

namespace CK.Setup
{

    /// <summary>
    /// Discovers available structure objects and instanciates them. 
    /// Once Types are registered, the <see cref="GetResult"/> method initializes the full object graph.
    /// </summary>
    public class StObjCollector
    {
        readonly AmbientContractCollector<StObjContextualMapper, StObjTypeInfo, MutableItem> _cc;
        readonly IStObjStructuralConfigurator _configurator;
        readonly IStObjValueResolver _valueResolver;
        readonly IActivityMonitor _monitor;
        readonly DynamicAssembly _tempAssembly;
        readonly IStObjRuntimeBuilder _runtimeBuilder;
        readonly Dictionary<string, object> _primaryRunCache;
        int _registerFatalOrErrorCount;

        /// <summary>
        /// Initializes a new <see cref="StObjCollector"/>.
        /// </summary>
        /// <param name="monitor">Logger to use. Can not be null.</param>
        /// <param name="traceDepencySorterInput">True to trace in <paramref name="monitor"/> the input of dependency graph.</param>
        /// <param name="traceDepencySorterOutput">True to trace in <paramref name="monitor"/> the sorted dependency graph.</param>
        /// <param name="runtimeBuilder">Runtime builder to use. A <see cref="BasicStObjBuilder"/> can be used.</param>
        /// <param name="dispatcher">Used to dispatch Types between contexts or hide them. See <see cref="IAmbientContractDispatcher"/>.</param>
        /// <param name="configurator">Used to configure items. See <see cref="IStObjStructuralConfigurator"/>.</param>
        /// <param name="valueResolver">Used to explicitely resolve or alter StObjConstruct parameters and object ambient properties. See <see cref="IStObjValueResolver"/>.</param>
        public StObjCollector(
            IActivityMonitor monitor,
            bool traceDepencySorterInput = false,
            bool traceDepencySorterOutput = false,
            IStObjRuntimeBuilder runtimeBuilder = null,
            IAmbientContractDispatcher dispatcher = null,
            IStObjStructuralConfigurator configurator = null,
            IStObjValueResolver valueResolver = null,
            Func<string,object> secondaryRunAccessor = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            _runtimeBuilder = runtimeBuilder ?? StObjContextRoot.DefaultStObjRuntimeBuilder;
            _monitor = monitor;
            if( secondaryRunAccessor != null ) _tempAssembly = new DynamicAssembly( secondaryRunAccessor );
            else
            {
                _primaryRunCache = new Dictionary<string, object>();
                _tempAssembly = new DynamicAssembly( _primaryRunCache );
            }
            _cc = new AmbientContractCollector<StObjContextualMapper, StObjTypeInfo, MutableItem>( _monitor, l => new StObjMapper(), ( l, p, t ) => new StObjTypeInfo( l, p, t ), _tempAssembly, dispatcher );
            _configurator = configurator;
            _valueResolver = valueResolver;
            if( traceDepencySorterInput ) DependencySorterHookInput = i => i.Trace( monitor );
            if( traceDepencySorterOutput ) DependencySorterHookOutput = i => i.Trace( monitor );
        }

        /// <summary>
        /// Gets the count of error or fatal that occurred during types registration.
        /// </summary>
        public int RegisteringFatalOrErrorCount => _registerFatalOrErrorCount;

        /// <summary>
        /// Gets ors sets whether the ordering of StObj that share the same rank in the dependency graph must be inverted.
        /// Defaults to false. (See <see cref="DependencySorter"/> for more information.)
        /// </summary>
        public bool RevertOrderingNames { get; set; }

        /// <summary>
        /// Registers types from multiple assemblies.
        /// Only classes and IPoco interfaces are considered.
        /// </summary>
        /// <param name="assemblyNames">The assembly names to register.</param>
        /// <returns>The number of new discovered classes.</returns>
        public int RegisterAssemblyTypes( IReadOnlyCollection<string> assemblyNames )
        {
            if( assemblyNames == null ) throw new ArgumentNullException( nameof( assemblyNames ) );
            int totalRegistered = 0;
            using( _monitor.OnError( () => ++_registerFatalOrErrorCount ) )
            using( _monitor.OpenTrace( $"Registering {assemblyNames.Count} assemblies." ) )
            {
                foreach( var one in assemblyNames )
                {
                    using( _monitor.OpenTrace( $"Registering assembly '{one}'." ) )
                    {
                        Assembly a = null;
                        try
                        {
                            a = Assembly.Load( one );
                        }
                        catch( Exception ex )
                        {
                            _monitor.Error( $"Error while loading assembly '{one}'.", ex );
                        }
                        int nbAlready = _cc.RegisteredTypeCount;
                        _cc.SafeRegister( a.GetTypes() );
                        int delta = _cc.RegisteredTypeCount - nbAlready;
                        _monitor.CloseGroup( $"{delta} types(s) registered." );
                        totalRegistered += delta;
                    }
                }
            }
            return totalRegistered;
        }

        /// <summary>
        /// Explicitely registers a class or a IPoco interface.
        /// </summary>
        /// <param name="t">Type to register.</param>
        /// <returns>True if it is a new class for this collector, false if it has already been registered.</returns>
        public void RegisterType( Type t )
        {
            if( t == null ) throw new ArgumentNullException( nameof( t ) );
            _monitor.Debug( $"Explicitely registering '{t.AssemblyQualifiedName}'." );
            using( _monitor.OnError( () => ++_registerFatalOrErrorCount ) )
            {
                try
                {
                    _cc.RegisterClassOrPoco( t );
                }
                catch( Exception ex )
                {
                    _monitor.Error( $"While registering type '{t.AssemblyQualifiedName}'.", ex );
                }
            }
        }

        /// <summary>
        /// Explicitely registers a set of classes or a IPoco interfaces.
        /// </summary>
        /// <param name="types">Types to register.</param>
        public void RegisterTypes( IReadOnlyCollection<Type> types )
        {
            if( types == null ) throw new ArgumentNullException( nameof( types ) );
            DoRegisterTypes( types, types.Count );
        }

        /// <summary>
        /// Explicitely registers a set of classes or a IPoco interfaces by their assembly qualified names.
        /// </summary>
        /// <param name="typeNames">Assembly qualified names of the types to register.</param>
        public void RegisterTypes( IReadOnlyCollection<string> typeNames )
        {
            if( typeNames == null ) throw new ArgumentNullException( nameof( typeNames ) );
            DoRegisterTypes( typeNames.Select( n => SimpleTypeFinder.WeakResolver( n, true ) ), typeNames.Count );
        }

        void DoRegisterTypes( IEnumerable<Type> types, int count )
        {
            if( types == null ) throw new ArgumentNullException();
            using( _monitor.OnError( () => ++_registerFatalOrErrorCount ) )
            using( _monitor.OpenTrace( $"Explicitely registering {count} type(s)." ) )
            {
                try
                {
                    foreach( var t in types )
                    {
                        _cc.RegisterClassOrPoco( t );
                    }
                }
                catch( Exception ex )
                {
                    _monitor.Error( ex );
                }
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
        /// Builds and returns a <see cref="StObjCollectorResult"/> if no error occurred during type registration.
        /// If <see cref="RegisteringFatalOrErrorCount"/> is not equal to 0, this throws a <see cref="CKException"/>.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <returns>The result.</returns>
        public StObjCollectorResult GetResult( IServiceProvider services )
        {
            if( services == null ) throw new ArgumentNullException( nameof( services ) );
            if( _registerFatalOrErrorCount > 0 )
            {
                throw new CKException( $"There are {_registerFatalOrErrorCount} registration errors." );
            }
            using( _monitor.OpenInfo( "Collecting all StObj information." ) )
            {
                AmbientContractCollectorResult<StObjContextualMapper,StObjTypeInfo,MutableItem> contracts;
                using( _monitor.OpenInfo( "Collecting Ambient Contracts, Type structure and Poco." ) )
                {
                    contracts = _cc.GetResult( services );
                    contracts.LogErrorAndWarnings( _monitor );
                }
                var stObjMapper = new StObjMapper();
                var result = new StObjCollectorResult( stObjMapper, contracts, _tempAssembly, _primaryRunCache );
                if( result.HasFatalError ) return result;
                using( _monitor.OpenInfo( "Creating Structure Objects." ) )
                {
                    int objectCount = 0;
                    foreach( StObjCollectorContextualResult r in result.Contexts )
                    {
                        using( _monitor.OnError( () => r.SetFatal() ) )
                        using( _monitor.OpenInfo( $"Working on Context [{r.Context}]." ) )
                        {
                            int nbItems = CreateMutableItems( r );
                            _monitor.CloseGroup( $"{nbItems} items created for {r.AmbientContractResult.ConcreteClasses.Count} types." );
                            objectCount += nbItems;
                        }
                    }
                    if( result.HasFatalError ) return result;
                    _monitor.CloseGroup( $"{objectCount} items created." );
                }

                IDependencySorterResult sortResult = null;
                using( _monitor.OpenInfo( "Handling dependencies." ) )
                {
                    bool noCycleDetected;
                    if( !PrepareDependentItems( result, out noCycleDetected ) )
                    {
                        _monitor.CloseGroup( "Prepare failed." );
                        Debug.Assert( result.HasFatalError );
                        return result;
                    }
                    if( !ResolvePreConstructAndPostBuildProperties( result ) )
                    {
                        _monitor.CloseGroup( "Resolving Ambient Properties failed." );
                        Debug.Assert( result.HasFatalError );
                        return result;
                    }
                    sortResult = DependencySorter.OrderItems(
                                                    _monitor,
                                                    result.AllMutableItems,
                                                    null,
                                                    new DependencySorterOptions()
                                                    {
                                                        SkipDependencyToContainer = true,
                                                        HookInput = DependencySorterHookInput,
                                                        HookOutput = DependencySorterHookOutput,
                                                        ReverseName = RevertOrderingNames
                                                    } );
                    Debug.Assert( sortResult.HasRequiredMissing == false,
                        "A missing requirement can not exist at this stage since we only inject existing Mutable items: missing unresolved dependencies are handled by PrepareDependentItems that logs Errors when needed." );
                    Debug.Assert( noCycleDetected || (sortResult.CycleDetected != null), "Cycle detected during item preparation => Cycle detected by the DependencySorter." );
                    if( !sortResult.IsComplete )
                    {
                        sortResult.LogError( _monitor );
                        _monitor.CloseGroup( "Ordering failed." );
                        result.SetFatal();
                        return result;
                    }
                }
                Debug.Assert( sortResult != null );

                // The structure objects have been ordered by their dependencies (and optionally
                // by the IStObjStructuralConfigurator). 
                // Their instance has been set during the first step (CreateMutableItems).
                // We can now call the StObjConstruct methods and returns an ordered list of IStObj.
                //
                using( _monitor.OnError( () => result.SetFatal() ) )
                using( _monitor.OpenInfo( "Initializing object graph." ) )
                {
                    int idxSpecialization = 0;
                    List<MutableItem> ordered = new List<MutableItem>();
                    foreach( ISortedItem sorted in sortResult.SortedItems )
                    {
                        var m = (MutableItem)sorted.Item;
                        // Calls StObjConstruct on Head for Groups.
                        if( m.ItemKind == DependentItemKindSpec.Item || sorted.IsGroupHead )
                        {
                            m.SetSorterData( ordered.Count, ref idxSpecialization, sorted.Requires, sorted.Children, sorted.Groups );
                            using( _monitor.OpenTrace( $"Constructing '{m.ToString()}'." ) )
                            {
                                try
                                {
                                    m.CallConstruct( _monitor, result.BuildValueCollector, _valueResolver );
                                }
                                catch( Exception ex )
                                {
                                    _monitor.Error( ex );
                                }
                            }
                            ordered.Add( m );
                        }
                        else
                        {
                            Debug.Assert( m.ItemKind != DependentItemKindSpec.Item && !sorted.IsGroupHead );
                            // We may call here a ConstructContent( IReadOnlyList<IStObj> packageContent ).
                            // But... is it a good thing for a package object to know its content detail?
                        }
                    }
                    using( _monitor.OpenInfo( "Setting Ambient Contracts." ) )
                    {
                        SetPostBuildProperties( result );
                    }
                    if( !result.HasFatalError ) result.SetSuccess( ordered );
                    return result;
                }
            }
        }

        /// <summary>
        /// Creates one or more StObjMutableItem for each ambient Type, each of them bound 
        /// to an instance created through its default constructor.
        /// This is the very first step.
        /// </summary>
        int CreateMutableItems( StObjCollectorContextualResult r )
        {
            IReadOnlyList<IReadOnlyList<MutableItem>> concreteClasses = r.AmbientContractResult.ConcreteClasses;
            int nbItems = 0;
            for( int i = concreteClasses.Count-1; i >= 0; --i )
            {
                IReadOnlyList<MutableItem> pathTypes = concreteClasses[i];
                Debug.Assert( pathTypes.Count > 0, "At least the final concrete class exists." );
                nbItems += pathTypes.Count;

                MutableItem specialization = r._specializations[i] = pathTypes[pathTypes.Count - 1];

                object theObject = specialization.CreateStructuredObject( _monitor, _runtimeBuilder );
                // If we failed to create an instance, we ensure that an error is logged and
                // continue the process.
                if( theObject == null )
                {
                    _monitor.Error( $"Unable to create an instance of '{pathTypes[pathTypes.Count - 1].AmbientTypeInfo.Type.FullName}'." );
                    continue;
                }
                // Finalize configuration by soliciting IStObjStructuralConfigurator.
                // It is important here to go top-down since specialized configuration 
                // should override more general ones.
                // Note that this works because we do NOT offer any access to Specialization 
                // in IStObjMutableItem. We actually could offer an access to the Generalization 
                // since it is configured, but it seems useless and may annoy us later.
                Debug.Assert( typeof( IStObjMutableItem ).GetProperty( "Generalization" ) == null );
                Debug.Assert( typeof( IStObjMutableItem ).GetProperty( "Specialization" ) == null );
                MutableItem generalization = pathTypes[0];
                MutableItem m = generalization;
                do
                {
                    m.ConfigureTopDown( _monitor, generalization );
                    if( _configurator != null ) _configurator.Configure( _monitor, m );
                }
                while( (m = m.Specialization) != null );
            }
            return nbItems;
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
                using( _monitor.OnError( () => contextResult.SetFatal() ) )
                using( _monitor.OpenInfo( $"Working on Context [{contextResult.Context}]." ) )
                {
                    foreach( MutableItem item in contextResult._specializations )
                    {
                        noCycleDetected &= item.PrepareDependentItem( _monitor, collector, contextResult );
                    }
                }
            }
            return !collector.HasFatalError;
        }

        /// <summary>
        /// This is the last step before ordering the dependency graph: all mutable items have now been created and configured, they are ready to be sorted,
        /// except that we must first resolve AmbiantProperties: computes TrackedAmbientProperties (and depending of the TrackAmbientPropertiesMode impact
        /// the requirements before sorting). This also gives IStObjValueResolver.ResolveExternalPropertyValue 
        /// a chance to configure unresolved properties. (Since this external resolution may provide a StObj, this may also impact the sort order).
        /// During this step, DirectProperties and AmbientContracts are also collected: all these properties are added to PreConstruct collectors
        /// or to PostBuild collector in order to always set a correctly constructed object to a property.
        /// </summary>
        bool ResolvePreConstructAndPostBuildProperties( StObjCollectorResult collector )
        {
            foreach( StObjCollectorContextualResult contextResult in collector.Contexts )
            {
                using( _monitor.OnError( () => contextResult.SetFatal() ) )
                using( _monitor.OpenInfo( $"Working on Context [{contextResult.Context}]." ) )
                {
                    foreach( MutableItem item in contextResult._specializations )
                    {
                        item.ResolvePreConstructAndPostBuildProperties( _monitor, collector, contextResult, _valueResolver );
                    }
                }
            }
            return !collector.HasFatalError;
        }

        /// <summary>
        /// Finalize construction by injecting Ambient Contracts objects and PostBuild Ambient Properties on specializations.
        /// </summary>
        bool SetPostBuildProperties( StObjCollectorResult collector )
        {
            foreach( StObjCollectorContextualResult contextResult in collector.Contexts )
            {
                using( _monitor.OnError( () => contextResult.SetFatal() ) )
                using( _monitor.OpenInfo( $"Working on Context [{contextResult.Context}]." ) )
                {
                    foreach( MutableItem item in contextResult._specializations )
                    {
                        item.SetPostBuildProperties( _monitor, collector, contextResult );
                    }
                }
            }
            return !collector.HasFatalError;
        }

    }

}
