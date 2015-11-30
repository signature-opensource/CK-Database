#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\StObjCollector.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
        readonly AmbientContractCollector<StObjContextualMapper,StObjTypeInfo,MutableItem> _cc;
        readonly IStObjStructuralConfigurator _configurator;
        readonly IStObjValueResolver _valueResolver;
        readonly IActivityMonitor _monitor;
        readonly DynamicAssembly _tempAssembly;
        readonly IStObjRuntimeBuilder _runtimeBuilder;
        int _registerFatalOrErrorCount;

        /// <summary>
        /// Initializes a new <see cref="StObjCollector"/>.
        /// </summary>
        /// <param name="monitor">Logger to use. Can not be null.</param>
        /// <param name="traceDepencySorterInput">True to trace in <paramref name="monitor"/> the input of dependency graph.</param>
        /// <param name="traceDepencySorterOutput">True to trace in <paramref name="monitor"/> the sorted dependency graph.</param>
        /// <param name="runtimeBuilder">Runtime builder to use. A <see cref="BasicStObjBuilder"/> can be used.</param>
        /// <param name="dispatcher">Used to dispatch Types betwenn contexts or hide them. See <see cref="IAmbientContractDispatcher"/>.</param>
        /// <param name="configurator">Used to configure items. See <see cref="IStObjStructuralConfigurator"/>.</param>
        /// <param name="valueResolver">Used to explicitely resolve or alter Construct parameters and object ambient properties. See <see cref="IStObjValueResolver"/>.</param>
        public StObjCollector( 
            IActivityMonitor monitor, 
            bool traceDepencySorterInput = false, 
            bool traceDepencySorterOutput = false, 
            IStObjRuntimeBuilder runtimeBuilder = null, 
            IAmbientContractDispatcher dispatcher = null, 
            IStObjStructuralConfigurator configurator = null,
            IStObjValueResolver valueResolver = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            _runtimeBuilder = runtimeBuilder ?? StObjContextRoot.DefaultStObjRuntimeBuilder;
            _monitor = monitor;
            _tempAssembly = new DynamicAssembly();
            _cc = new AmbientContractCollector<StObjContextualMapper,StObjTypeInfo, MutableItem>( _monitor, l => new StObjMapper(), ( l, p, t ) => new StObjTypeInfo( l, p, t ), _tempAssembly, dispatcher );
            _configurator = configurator;
            _valueResolver = valueResolver;
            if( traceDepencySorterInput ) DependencySorterHookInput = i => i.Trace( monitor );
            if( traceDepencySorterOutput ) DependencySorterHookOutput = i => i.Trace( monitor );
        }

        /// <summary>
        /// Gets the count of error or fatal that occurred during <see cref="RegisterTypes"/> or <see cref="RegisterClass"/> calls.
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
        /// Gets ors sets whether the ordering of StObj that share the same rank in the dependency graph must be inverted.
        /// Defaults to false. (See <see cref="DependencySorter"/> for more information.)
        /// </summary>
        public bool RevertOrderingNames { get; set; }

        /// <summary>
        /// Registers types discovered by an <see cref="AssemblyRegisterer"/>.
        /// </summary>
        /// <param name="registerer">The discoverer that contains assemblies/types.</param>
        /// <returns>The number of new discovered classes.</returns>
        public int RegisterTypes( AssemblyRegisterer registerer )
        {
            if( registerer == null ) throw new ArgumentNullException( "registerer" );
            int totalRegistered = 0;
            using( _monitor.OnError( () => ++_registerFatalOrErrorCount ) )
            using( _monitor.OpenTrace().Send( "Registering {0} assemblies.", registerer.Assemblies.Count ) )
            {
                foreach( var one in registerer.Assemblies )
                {
                    using( _monitor.OpenTrace().Send( "Registering assembly '{0}'.", one.Assembly.FullName ) )
                    {
                        int nbAlready = _cc.RegisteredTypeCount;
                        _cc.Register( one.Types );
                        int delta = _cc.RegisteredTypeCount - nbAlready;
                        _monitor.CloseGroup( String.Format( "{0} types(s) registered.", delta ) );
                        totalRegistered += delta;
                    }
                }
            }
            return totalRegistered;
        }

        /// <summary>
        /// Explicitely registers a class.
        /// </summary>
        /// <param name="c">Class to register.</param>
        /// <returns>True if it is a new class for this collector, false if it has already been registered.</returns>
        public bool RegisterClass( Type c )
        {
            using( _monitor.OpenTrace().Send( "Explicitely registering Type '{0}'.", c.AssemblyQualifiedName ) )
            using( _monitor.OnError( () => ++_registerFatalOrErrorCount ) )
            {
                if( !_cc.RegisterClass( c ) )
                {
                    _monitor.CloseGroup( "Already registered." );
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Explicitely registers a set of class by their assembly qualified names.
        /// </summary>
        /// <param name="classes">Assembly qualified names of the classes to register.</param>
        public void RegisterClasses( IReadOnlyList<string> classes )
        {
            if( classes == null ) throw new ArgumentNullException();
            using( _monitor.OpenTrace().Send( "Explicitely registering {0} class(es).", classes.Count ) )
            {
                foreach( var aqn in classes )
                {
                    try
                    {
                        RegisterClass( SimpleTypeFinder.WeakDefault.ResolveType( aqn, true ) );
                    }
                    catch( Exception ex )
                    {
                        ++_registerFatalOrErrorCount;
                        _monitor.OpenError().Send( ex, "While resolving type '{0}'.", aqn );
                    }
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
        /// To ignore registering errors, calls <see cref="ClearRegisteringErrors"/> before calling this method.
        /// </summary>
        /// <returns>The result.</returns>
        public StObjCollectorResult GetResult()
        {
            if( _registerFatalOrErrorCount > 0 )
            {
                throw new CKException( "There are {0} registration errors. ClearRegisteringErrors must be called before calling this GetResult method to ignore registration errors.", _registerFatalOrErrorCount );
            }
            using( _monitor.OpenInfo().Send( "Collecting all StObj information." ) )
            {
                AmbientContractCollectorResult<StObjContextualMapper,StObjTypeInfo,MutableItem> contracts;
                using( _monitor.OpenInfo().Send( "Collecting Ambient Contracts and Type structure." ) )
                {
                    contracts = _cc.GetResult();
                    contracts.LogErrorAndWarnings( _monitor );
                }
                var stObjMapper = new StObjMapper();
                var result = new StObjCollectorResult( stObjMapper, contracts );
                if( result.HasFatalError ) return result;

                using( _monitor.OpenInfo().Send( "Creating Structure Objects." ) )
                {
                    int objectCount = 0;
                    foreach( StObjCollectorContextualResult r in result.Contexts )
                    {
                        using( _monitor.OnError( () => r.SetFatal() ) )
                        using( _monitor.OpenInfo().Send( "Working on Context [{0}].", r.Context ) )
                        {
                            int nbItems = CreateMutableItems( r );
                            _monitor.CloseGroup( String.Format( " {0} items created for {1} types.", nbItems, r.AmbientContractResult.ConcreteClasses.Count ) );
                            objectCount += nbItems;
                        }
                    }
                    if( result.HasFatalError ) return result;
                    _monitor.CloseGroup( String.Format( "{0} items created.", objectCount ) );
                }

                IDependencySorterResult sortResult = null;
                using( _monitor.OpenInfo().Send( "Handling dependencies." ) )
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
                    sortResult = DependencySorter.OrderItems( result.AllMutableItems, null, new DependencySorterOptions()
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
                // We can now call the Construct methods and returns an ordered list of IStObj.
                //
                using( _monitor.OnError( () => result.SetFatal() ) )
                using( _monitor.OpenInfo().Send( "Initializing object graph." ) )
                {
                    int idxSpecialization = 0;
                    List<MutableItem> ordered = new List<MutableItem>();
                    foreach( ISortedItem sorted in sortResult.SortedItems )
                    {
                        var m = (MutableItem)sorted.Item;
                        // Calls Construct on Head for Groups.
                        if( m.ItemKind == DependentItemKindSpec.Item || sorted.IsGroupHead )
                        {
                            m.SetSorterData( ordered.Count, ref idxSpecialization, sorted.Requires, sorted.Children, sorted.Groups );
                            using( _monitor.OpenTrace().Send( "Constructing '{0}'.", m.ToString() ) )
                            {
                                try
                                {
                                    m.CallConstruct( _monitor, result.BuildValueCollector, _valueResolver );
                                }
                                catch( Exception ex )
                                {
                                    _monitor.Error().Send( ex );
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
                    using( _monitor.OpenInfo().Send( "Setting Ambient Contracts." ) )
                    {
                        SetPostBuildProperties( result );
                    }
                    if( !result.HasFatalError ) result.SetSuccess( ordered.ToReadOnlyList() );
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
                    _monitor.Error().Send( "Unable to create an instance of '{0}'.", pathTypes[pathTypes.Count - 1].AmbientTypeInfo.Type.FullName );
                    continue;
                }
                // Finalize configuration by soliciting IStObjStructuralConfigurator.
                // It is important here to go top-down since specialized configuration 
                // should override more general ones.
                // Note that this works because we do NOT offer any access to Specialization 
                // in IStObjMutableItem. We actually could offer an access to the Generalization 
                // since it is configured, but it seems useless and may block us later.
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
                using( _monitor.OpenInfo().Send( "Working on Context [{0}].", contextResult.Context ) )
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
                using( _monitor.OpenInfo().Send( "Working on Context [{0}].", contextResult.Context ) )
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
                using( _monitor.OpenInfo().Send( "Working on Context [{0}].", contextResult.Context ) )
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
