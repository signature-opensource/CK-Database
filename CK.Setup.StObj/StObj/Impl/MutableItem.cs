using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;
using System.Diagnostics;
using System.Collections;

namespace CK.Setup
{

    partial class MutableItem : StObjContextTypeInfo, IStObj, IStObjMutableItem, IDependentItemContainerTyped, IDependentItemContainerRef
    {
        class LeafData
        {
            public LeafData( MutableItem leaf, List<MutableAmbientProperty> ap, MutableAmbientContract[] ac )
            {
                LeafSpecialization = leaf;
                AllAmbientProperties = ap;
                AllAmbientContracts = ac;
            }

            /// <summary>
            /// Useless to store it at each level.
            /// </summary>
            public readonly MutableItem LeafSpecialization;
            
            /// <summary>
            /// Ambient Properties are shared by the inheritance chain (it is
            /// not null only at the specialization level).
            /// It is a List because we use it as a cache for propagation of ambient properties (in 
            /// EnsureCachedAmbientProperty): new properties issued from Container or Generalization are added 
            /// and cached into this list.
            /// </summary>
            public readonly List<MutableAmbientProperty> AllAmbientProperties;
            /// <summary>
            /// Like Ambient Properties above, Ambient Contracts are shared by the inheritance chain (it is
            /// not null only at the specialization level), but can use here an array instead of a dynamic list
            /// since there is no caching needed. Each MutableAmbientContract here is bound to its AmbientContractInfo
            /// in the StObjTypeinfo.AmbientContracts.
            /// </summary>
            public readonly MutableAmbientContract[] AllAmbientContracts;

            // _directPropertiesToSet is not null only on _leafSpecialization and is allocated
            // if and only if needed (by SetDirectPropertyValue).
            public Dictionary<PropertyInfo,object> DirectPropertiesToSet;

            /// <summary>
            /// Available only at the leaf level.
            /// </summary>
            public object StructuredObject;

            /// <summary>
            /// Concern the final type.
            /// </summary>
            public ImplementableTypeInfo ImplementableTypeInfo;

            public Type StubType;

            /// <summary>
            /// Useless to store it at each level.
            /// </summary>
            public MutableItem RootGeneralization;

        }

        readonly LeafData _leafData;

        // This is available at any level thanks to the ordering of ambient properties
        // and the ListAmbientProperty that exposes only the start of the list: only the 
        // properties that are available at the level appear in the list.
        // (This is the same for AmbientContracts.)
        readonly IReadOnlyList<MutableAmbientProperty> _ambientPropertiesEx;
        readonly IReadOnlyList<MutableAmbientContract> _ambientContractsEx;

        MutableItem _generalization;

        MutableReference _container;
        MutableReferenceList _requires;
        MutableReferenceList _requiredBy;
        MutableReferenceList _children;
        MutableReferenceList _groups;
        
        IReadOnlyList<MutableParameter> _constructParameterEx;
        DependentItemKind _itemKind;
        List<StObjProperty> _stObjProperties;

        string _dFullName;
        MutableItem _dContainer;
        IReadOnlyList<MutableItem> _dRequires;
        IReadOnlyList<MutableItem> _dRequiredBy;
        IReadOnlyList<MutableItem> _dChildren;
        IReadOnlyList<MutableItem> _dGroups;
        // Our container comes from the configuration of this item or is inherited (from generalization). 
        bool IsOwnContainer { get { return _dContainer != null && _dContainer.ObjectType == _container.Type; } }

        // The tracking mode for ambient properties is inherited and nothing prevents it to 
        // change between levels (a Generalization can set AddPropertyHolderAsChildren and a Specialization 
        // define PropertyHolderRequiredByThis, even if that seems pretty strange and that I can not imagine any
        // clever use of such beast...). Anyway, technically speaking, it has to work this way.
        TrackAmbientPropertiesMode _trackAmbientPropertiesMode;
        // Ambient properties are per StObj.
        List<TrackedAmbientPropertyInfo> _trackedAmbientProperties;
        IReadOnlyList<TrackedAmbientPropertyInfo> _trackedAmbientPropertiesEx;
        // True if this or any Generalization has _trackAmbientPropertiesMode != None.
        bool _needsTrackedAmbientProperties;

        enum PrepareState : byte
        {
            None,
            RecursePreparing,
            PreparedDone,
            CachingAmbientProperty
        }
        PrepareState _prepareState;

        /// <summary>
        /// Used only for Empty Object Pattern implementations.
        /// </summary>
        internal MutableItem()
        {
        }

        /// <summary>
        /// Called from Specialization up to Generalization.
        /// </summary>
        internal MutableItem( StObjTypeInfo objectType, string context, MutableItem specialization )
            : base(objectType, context, specialization ) 
        {
            Debug.Assert( context != null );
            if( Specialization != null )
            {
                Debug.Assert( Specialization._generalization == null );
                Specialization._generalization = this;
                _leafData = Specialization._leafData;
            }
            else
            {
                var ap = AmbientTypeInfo.AmbientProperties.Select( p => new MutableAmbientProperty( this, p ) ).ToList();
                var ac = new MutableAmbientContract[AmbientTypeInfo.AmbientContracts.Count];
                for( int i = ac.Length-1; i >= 0; --i )
                {
                    ac[i] = new MutableAmbientContract( this, AmbientTypeInfo.AmbientContracts[i] );
                }
                _leafData = new LeafData( this, ap, ac );
            }
            _ambientPropertiesEx = new ListAmbientProperty( this );
            _ambientContractsEx = new ListAmbientContract( this );
        }

        protected internal override bool AbstractTypeCanBeInstanciated( IActivityLogger logger, DynamicAssembly assembly )
        {
            Debug.Assert( Specialization == null && Type.IsAbstract );
            Debug.Assert( _leafData.ImplementableTypeInfo == null, "Only called once." );
            _leafData.ImplementableTypeInfo = ImplementableTypeInfo.GetImplementableTypeInfo( logger, Type, this );
            if( _leafData.ImplementableTypeInfo != null )
            {
                _leafData.StubType = assembly.CreateStubType( logger, _leafData.ImplementableTypeInfo );
                return true;
            }
            return false;
        }

        #region Configuration

        public object CreateInstance( IActivityLogger logger )
        {
            Debug.Assert( Specialization == null );
            Debug.Assert( _leafData.StructuredObject == null, "Called once and only once." );
            Debug.Assert( (_leafData.StubType ?? Type) != null, "If no type, AbstractTypeCanBeInstanciated returned false." );
            try
            {
                return _leafData.StructuredObject = Activator.CreateInstance( _leafData.StubType ?? Type );
            }
            catch( Exception ex )
            {
                logger.Error( ex );
                return null;
            }
        }

        internal void ConfigureTopDown( IActivityLogger logger, MutableItem rootGeneralization )
        {
            Debug.Assert( _leafData.RootGeneralization == null || _leafData.RootGeneralization == rootGeneralization );
            Debug.Assert( (rootGeneralization == this) == (_generalization == null) );

            _leafData.RootGeneralization = rootGeneralization;
            ApplyTypeInformation( logger );
            AnalyseConstruct( logger );
            ConfigureFromAttributes( logger );
        }

        void ApplyTypeInformation( IActivityLogger logger )
        {
            Debug.Assert( _container == null, "Called only once right after object instanciation." );

            _container = new MutableReference( this, StObjMutableReferenceKind.Container );
            _container.Type = AmbientTypeInfo.Container;
            _container.Context = AmbientTypeInfo.ContainerContext;
            _itemKind = AmbientTypeInfo.ItemKind;

            if( AmbientTypeInfo.StObjProperties.Count > 0 ) _stObjProperties = AmbientTypeInfo.StObjProperties.Select( sp => new StObjProperty( sp ) ).ToList();

            // StObjTypeInfo already applied inheritance of TrackAmbientProperties attribute accross StObj levels.
            // But since TrackAmbientProperties is "mutable" (can be configured), we only know its actual value once PrepareDependentItem has done its job:
            // inheritance by StObjType onky gives the IStObjStructuralConfigurator a more precise information.
            _trackAmbientPropertiesMode = AmbientTypeInfo.TrackAmbientProperties;
            _requires = new MutableReferenceList( this, StObjMutableReferenceKind.Requires );
            if( AmbientTypeInfo.Requires != null )
            {
                _requires.AddRange( AmbientTypeInfo.Requires.Select( t => new MutableReference( this, StObjMutableReferenceKind.Requires ) { Type = t, Context = AmbientTypeInfo.FindContextFromMapAttributes( t ) } ) );
            }
            _requiredBy = new MutableReferenceList( this, StObjMutableReferenceKind.RequiredBy );
            if( AmbientTypeInfo.RequiredBy != null )
            {
                _requiredBy.AddRange( AmbientTypeInfo.RequiredBy.Select( t => new MutableReference( this, StObjMutableReferenceKind.RequiredBy ) { Type = t, Context = AmbientTypeInfo.FindContextFromMapAttributes( t ) } ) );
            }
            _children = new MutableReferenceList( this, StObjMutableReferenceKind.Child );
            if( AmbientTypeInfo.Children != null )
            {
                _children.AddRange( AmbientTypeInfo.Children.Select( t => new MutableReference( this, StObjMutableReferenceKind.RequiredBy ) { Type = t, Context = AmbientTypeInfo.FindContextFromMapAttributes( t ) } ) );
            }
            _groups = new MutableReferenceList( this, StObjMutableReferenceKind.Group );
            if( AmbientTypeInfo.Groups != null )
            {
                _groups.AddRange( AmbientTypeInfo.Groups.Select( t => new MutableReference( this, StObjMutableReferenceKind.Group ) { Type = t, Context = AmbientTypeInfo.FindContextFromMapAttributes( t ) } ) );
            }
        }

        void AnalyseConstruct( IActivityLogger logger )
        {
            Debug.Assert( _constructParameterEx == null, "Called only once right after object instanciation..." );
            Debug.Assert( _container != null, "...and after ApplyTypeInformation." );

            if( AmbientTypeInfo.Construct != null && AmbientTypeInfo.ConstructParameters.Length > 0 )
            {
                var parameters = new MutableParameter[AmbientTypeInfo.ConstructParameters.Length];
                for( int idx = 0; idx < parameters.Length; ++idx )
                {
                    ParameterInfo cp = AmbientTypeInfo.ConstructParameters[idx];
                    bool isContainer = idx == AmbientTypeInfo.ContainerConstructParameterIndex;
                    MutableParameter p = new MutableParameter( this, cp, isContainer );
                    p.Context = AmbientTypeInfo.ConstructParameterTypedContext[idx];
                    if( isContainer )
                    {
                        // Sets the _container to be the parameter object.
                        _container = p;
                    }
                    parameters[idx] = p;
                }
                _constructParameterEx = new ReadOnlyListOnIList<MutableParameter>( parameters );
            }
            else
            {
                _constructParameterEx = ReadOnlyListEmpty<MutableParameter>.Empty;
            }
        }

        void ConfigureFromAttributes( IActivityLogger logger )
        {
            foreach( var c in GetCustomAttributes<IStObjStructuralConfigurator>() )
            {
                c.Configure( logger, this );
            }
        }


        #endregion

        internal MutableItem Generalization
        {
            get { return _generalization; }
        }

        internal new MutableItem Specialization
        {
            get { return (MutableItem)base.Specialization; }
        }

        #region IStObjMutableItem is called during Configuration

        DependentItemKind IStObjMutableItem.ItemKind
        {
            get { return _itemKind; }
            set { _itemKind = value; }
        }

        TrackAmbientPropertiesMode IStObjMutableItem.TrackAmbientProperties
        {
            get { return _trackAmbientPropertiesMode; }
            set { _trackAmbientPropertiesMode = value; }
        }

        IStObjMutableReference IStObjMutableItem.Container { get { return _container; } }

        IStObjMutableReferenceList IStObjMutableItem.Children { get { return _children; } }

        IStObjMutableReferenceList IStObjMutableItem.Requires { get { return _requires; } }

        IStObjMutableReferenceList IStObjMutableItem.RequiredBy { get { return _requiredBy; } }

        IStObjMutableReferenceList IStObjMutableItem.Groups { get { return _groups; } }

        IReadOnlyList<IStObjMutableParameter> IStObjMutableItem.ConstructParameters { get { return _constructParameterEx; } }

        IReadOnlyList<IStObjAmbientProperty> IStObjMutableItem.SpecializedAmbientProperties { get { return _ambientPropertiesEx; } }

        IReadOnlyList<IStObjMutableAmbientContract> IStObjMutableItem.SpecializedAmbientContracts { get { return _ambientContractsEx; } }

        bool IStObjMutableItem.SetDirectPropertyValue( IActivityLogger logger, string propertyName, object value, string sourceDescription )
        {
            if( logger == null ) throw new ArgumentNullException( "logger", "Source:" + sourceDescription );
            if( String.IsNullOrEmpty( propertyName ) ) throw new ArgumentException( "Can not ne null nor empty. Source:" + sourceDescription, "propertyName" );
            if( value == Type.Missing ) throw new ArgumentException( "Setting property to Type.Missing is not allowed. Source:" + sourceDescription, "value" );

            // Is it an Ambient property?
            // If yes, it is an error... 
            // We may consider that it is an error if the property is defined at this type level (or above), 
            // and a simple warning if the property is defined by a specialization (the developper may not be aware of it).
            // Note: since we check properties' type homogeneity in StObjTypeInfo, an Ambient/StObj/Direct property is always 
            // of the same "kind" regardless of its owner specialization depth.
            MutableAmbientProperty mp = _leafData.AllAmbientProperties.FirstOrDefault( a => a.Name == propertyName );
            if( mp != null )
            {
                logger.Error( "Unable to set direct property '{1}.{0}' since it is defined as an Ambient property. Use SetAmbiantPropertyValue to set it. (Source:{2})", propertyName, Type.FullName, sourceDescription );
                return false;
            }

            // Direct property set.
            // Targets the specialization to honor property covariance.
            PropertyInfo p = _leafData.LeafSpecialization.Type.GetProperty( propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null, Type.EmptyTypes, null );
            if( p == null || !p.CanWrite )
            {
                logger.Error( "Unable to set direct property '{1}.{0}' structural value. It must exist and be writeable (on type '{2}'). (Source:{3})", propertyName, Type.FullName, _leafData.LeafSpecialization.Type.FullName, sourceDescription );
                return false;
            }
            if( _leafData.DirectPropertiesToSet == null ) _leafData.DirectPropertiesToSet = new Dictionary<PropertyInfo, object>();
            _leafData.DirectPropertiesToSet[p] = value;
            return true;
        }

        bool IStObjMutableItem.SetAmbiantPropertyValue( IActivityLogger logger, string propertyName, object value, string sourceDescription )
        {
            if( logger == null ) throw new ArgumentNullException( "logger", "Source:" + sourceDescription );
            if( String.IsNullOrEmpty( propertyName ) ) throw new ArgumentException( "Can not ne null nor empty. Source:" + sourceDescription, "propertyName" );
            if( value == Type.Missing ) throw new ArgumentException( "Setting property to Type.Missing is not allowed. Source:" + sourceDescription, "value" );

            // Is it an Ambient property?
            // If yes, set the value onto the property.
            MutableAmbientProperty mp = _leafData.AllAmbientProperties.FirstOrDefault( a => a.Name == propertyName );
            if( mp != null )
            {
                return mp.SetValue( AmbientTypeInfo.SpecializationDepth, logger, value );
            }
            logger.Error( "Unable to set unexisting Ambient property '{1}.{0}'. It must exist, be writeable and marked with AmbientPropertyAttribute. (Source:{2})", propertyName, Type.FullName, sourceDescription );
            return false;
        }

        bool IStObjMutableItem.SetAmbiantPropertyConfiguration( IActivityLogger logger, string propertyName, string context, Type type, StObjRequirementBehavior behavior, string sourceDescription )
        {
            if( logger == null ) throw new ArgumentNullException( "logger", "Source:" + sourceDescription );
            if( String.IsNullOrEmpty( propertyName ) ) throw new ArgumentException( "Can not ne null nor empty. Source:" + sourceDescription, "propertyName" );

            MutableAmbientProperty mp = _leafData.AllAmbientProperties.FirstOrDefault( a => a.Name == propertyName );
            if( mp != null )
            {
                return mp.SetConfiguration( AmbientTypeInfo.SpecializationDepth, logger, context, type, behavior );
            }
            logger.Error( "Unable to configure unexisting Ambient property '{1}.{0}'. It must exist, be writeable and marked with AmbientPropertyAttribute. (Source:{2})", propertyName, Type.FullName, sourceDescription );
            return false;        
        }

        bool IStObjMutableItem.SetStObjPropertyValue( IActivityLogger logger, string propertyName, object value, string sourceDescription )
        {
            if( logger == null ) throw new ArgumentNullException( "logger", "Source:" + sourceDescription );
            if( String.IsNullOrEmpty( propertyName ) ) throw new ArgumentException( "Can not ne null nor empty. Source:" + sourceDescription, "propertyName" );
            if( value == Type.Missing ) throw new ArgumentException( "Setting property to Type.Missing is not allowed. Source:" + sourceDescription, "value" );

            MutableAmbientProperty mp = _leafData.AllAmbientProperties.FirstOrDefault( a => a.Name == propertyName );
            if( mp != null )
            {
                logger.Error( "Unable to set StObj property '{1}.{0}' since it is defined as an Ambient property. Use SetAmbiantPropertyValue to set it. (Source:{2})", propertyName, Type.FullName, sourceDescription );
                return false;
            }

            SetStObjProperty( propertyName, value );
            return true;
        }

        #endregion

        internal bool PrepareDependendtItem( IActivityLogger logger, IStObjValueResolver dependencyResolver, StObjCollectorResult collector, StObjCollectorContextualResult cachedCollector )
        {
            if( _prepareState == PrepareState.PreparedDone ) return true;
            using( logger.OpenGroup( LogLevel.Trace, "Preparing '{0}'.", ToString() ) )
            {
                try
                {
                    bool result = true;
                    if( _prepareState == PrepareState.RecursePreparing )
                    {
                        logger.Warn( "Cycle detected while preparing item." );
                        result = false;
                    }
                    else
                    {
                        _prepareState = PrepareState.RecursePreparing;
                        
                        ResolveDirectReferences( logger, collector, cachedCollector );
                        if( _dContainer != null ) result &= _dContainer.PrepareDependendtItem( logger, dependencyResolver, collector, cachedCollector );
                        // Prepares Generalization and inherits from it as needed.
                        if( _generalization != null )
                        {
                            result &= _generalization.PrepareDependendtItem( logger, dependencyResolver, collector, cachedCollector );
                            if( _dContainer == null ) _dContainer = _generalization._dContainer;
                            if( _itemKind == DependentItemKind.Unknown ) _itemKind = _generalization._itemKind;
                            if( _trackAmbientPropertiesMode == TrackAmbientPropertiesMode.Unknown ) _trackAmbientPropertiesMode = _generalization._trackAmbientPropertiesMode;
                            _needsTrackedAmbientProperties = _generalization._needsTrackedAmbientProperties;
                        }
                        // Check configuration.
                        if( _itemKind == DependentItemKind.Unknown )
                        {
                            logger.Warn( "ItemKind is not specified. It defaults to SimpleItem. It should be set explicitely to either SimpleItem, Group or Container." );
                            _itemKind = DependentItemKind.Item;
                        }
                        if( _trackAmbientPropertiesMode == TrackAmbientPropertiesMode.Unknown ) _trackAmbientPropertiesMode = TrackAmbientPropertiesMode.None;
                        
                        // Allocates Ambient Properties now that we know the final configuration for it.
                        Debug.Assert( _trackAmbientPropertiesMode != TrackAmbientPropertiesMode.Unknown );
                        if( _trackAmbientPropertiesMode != TrackAmbientPropertiesMode.None )
                        {
                            _trackedAmbientProperties = new List<TrackedAmbientPropertyInfo>();
                            _needsTrackedAmbientProperties = true;
                        }
                        // We can handle StObjProperties (check type coherency and propagate values) since the Container and 
                        // the Generalization have been prepared, StObj properties can safely be located and propagated to this StObj.
                        CheckStObjProperties( logger );

                        // For AmbientProperties, this can not be done the same way: Ambient Properties are "projected to the leaf": they 
                        // have to be managed at the most specialized level: this is done in the next preparation step.
                    }
                    return result;

                }
                finally
                {
                    _prepareState = PrepareState.PreparedDone;
                }
            }
        }

        bool ResolveDirectReferences( IActivityLogger logger, StObjCollectorResult collector, StObjCollectorContextualResult cachedCollector )
        {
            Debug.Assert( _container != null && _constructParameterEx != null );
            bool result = true;
            _dFullName = AmbientContractCollector.FormatContextualFullName( Context, Type );
            _dContainer = _container.ResolveToStObj( logger, collector, cachedCollector );
            // Requirement intialization.
            HashSet<MutableItem> req = new HashSet<MutableItem>();
            {
                // Requires are... Required (when not configured as optional by IStObjStructuralConfigurator).
                foreach( MutableItem dep in _requires.AsList.Select( r => r.ResolveToStObj( logger, collector, cachedCollector ) ) )
                {
                    if( dep != null ) req.Add( dep );
                }
                // Construct parameters are Required... except:
                // - If they are one of our Container but this is handled
                //   at the DependencySorter level by using the SkipDependencyToContainer option.
                //   See the commented old code (to be kept) below for more detail on this option.
                // - If IStObjMutableParameter.SetParameterValue has been called by a IStObjStructuralConfigurator, then this 
                //   breaks the potential dependency.
                // 
                if( _constructParameterEx.Count > 0 )
                {
                    foreach( MutableParameter t in _constructParameterEx )
                    {
                        if( !t.HasBeenSet )
                        {
                            MutableItem dep = t.ResolveToStObj( logger, collector, cachedCollector );
                            if( dep != null ) req.Add( dep );
                        }
                    }
                }
            }
            // This will be updated after the Sort with clean Requirements (no Generalization nor Containers in it).
            _dRequires = req.ToReadOnlyList();

            // RequiredBy initialization.
            if( _requiredBy.Count > 0 )
            {
                _dRequiredBy = _requiredBy.AsList.Select( r => r.ResolveToStObj( logger, collector, cachedCollector ) ).Where( m => m != null ).ToReadOnlyList();
            }
            // Children Initialization.
            if( _children.Count > 0 )
            {
                _dChildren = _children.AsList.Select( r => r.ResolveToStObj( logger, collector, cachedCollector ) ).Where( m => m != null ).ToReadOnlyList();
            }
            // Groups Initialization.
            if( _groups.Count > 0 )
            {
                _dGroups = _groups.AsList.Select( r => r.ResolveToStObj( logger, collector, cachedCollector ) ).Where( m => m != null ).ToReadOnlyList();
            }
            return result;
        }

        #region (Old fully commented PrepareDependentItem code to be kept for documentation - SkipDependencyToContainer option rationale).
        //internal void PrepareDependentItem( IActivityLogger logger, StObjCollectorResult result, StObjCollectorContextualResult contextResult )
        //{
        //    Debug.Assert( _container != null && _constructParameterEx != null );
        //    Debug.Assert( _context == contextResult.Context && result[_context] == contextResult, "We are called inside our typed context, this avoids the lookup result[Context] to obtain the owner's context (the default)." );

        //    // Container initialization.
        //    //
        //    // Since we want to remove all the containers of the object from its parameter requirements (see below), 
        //    // we can not rely on the DependencySorter to detect a cyclic chain of containers:
        //    // we use the list to collect the chain of containers and detect cycles.
        //    List<MutableItem> allContainers = null;
        //    ComputeFullNameAndResolveContainer( logger, result, contextResult, ref allContainers );

        //    // Requirement intialization.
        //    HashSet<MutableItem> req = new HashSet<MutableItem>();
        //    {
        //        // Requires are... Required (when not configured as optional by IStObjStructuralConfigurator).
        //        foreach( MutableItem dep in _requires.Select( r => r.ResolveToStObj( logger, result, contextResult ) ).Where( m => m != null ) )
        //        {
        //            req.Add( dep );
        //        }
        //        // Construct parameters are Required... except if they are one of our Container.
        //        if( _constructParameterEx.Count > 0 )
        //        {
        //            // We are here considering here that a Container parameter does NOT define a dependency to the whole container (with its content):
        //            //
        //            //      That seems strange: we may expect the container to be fully initialized when used as a parameter by a dependency Construct...
        //            //      The fact is that we are dealing with Objects that have a method Construct, that this Construct method is called on the head
        //            //      of the container (before any of its content) and that this method has no "thickness", no content in terms of dependencies: its
        //            //      execution fully initializes the StOj and we can use it.
        //            //      Construct method is a requirement on "Init", not on "InitContent".
        //            //      This is actually fully coherent with the way the setup works. An item of a package does not "require" its own package, it is 
        //            //      contained in its package and can require items in the package as it needs.
        //            // 
        //            foreach( MutableParameter t in _constructParameterEx )
        //            {
        //                // Do not consider the container as a requirement since a Container is
        //                // already a dependency (on the head's Container) and that a requirement on a container
        //                // targets the whole content of it (this would lead to a cycle in the dependency graph).
        //                MutableItem dep = t.ResolveToStObj( logger, result, contextResult );
        //                if( dep != null && (allContainers == null || allContainers.Contains( dep ) == false) ) req.Add( dep );
        //            }
        //        }
        //    }
        //    _dRequires = req.ToReadOnlyList();

        //    // RequiredBy initialization.
        //    if( _requiredBy.Count > 0 )
        //    {
        //        _dRequiredBy = _requiredBy.Select( r => r.ResolveToStObj( logger, result, contextResult ) ).Where( m => m != null ).ToReadOnlyList();
        //    }
        //}

        //void ComputeFullNameAndResolveContainer( IActivityLogger logger, StObjCollectorResult result, StObjCollectorContextualResult contextResult, ref List<MutableItem> prevContainers )
        //{
        //    if( _dFullName != null ) return;

        //    _dFullName = AmbientContractCollector.DisplayName( _context, _objectType.Type );
        //    _dContainer = _container.ResolveToStObj( logger, result, contextResult );

        //    // Since we are obliged here to do in advance what the SetupOrderer will do (to remove dependencies to containers, see PrepareDependentItem above),
        //    // we must apply the "Container inheritance"...
            
        //    // TODO... Here or in DependencySorter... ?
        //    //    All this Container discovering stuff duplicates DependencySorter work...
        //    //    
        //    // => Answer: Done in the dependency sorter.

        //    if( _dContainer != null )
        //    {
        //        _dContainer._hasBeenReferencedAsAContainer = true;
        //        if( prevContainers == null ) prevContainers = new List<MutableItem>();
        //        else if( prevContainers.Contains( _dContainer ) )
        //        {
        //            logger.Fatal( "Recursive Container chain encountered: '{0}'.", String.Join( "', '", prevContainers.Select( m => m._dFullName ) ) );
        //            return;
        //        }
        //        prevContainers.Add( _dContainer );
        //        Type containerContext = _dContainer.Context;
        //        if( containerContext != contextResult.Context )
        //        {
        //            contextResult = result[containerContext];
        //            Debug.Assert( contextResult != null );
        //        }
        //        _dContainer.ComputeFullNameAndResolveContainer( logger, result, contextResult, ref prevContainers );
        //    }
        //}
        #endregion

        /// <summary>
        /// Called by StObjCollector once the mutable items have been sorted.
        /// </summary>
        /// <param name="idx">The <see cref="IndexOrdered"/>.</param>
        /// <param name="requiresFromSorter">Cleaned up requirements (no Generalization nor Containers).</param>
        internal void SetSorterData( int idx, IEnumerable<ISortedItem> requiresFromSorter, IEnumerable<ISortedItem> childrenFromSorter, IEnumerable<ISortedItem> groupsFromSorter )
        {
            Debug.Assert( IndexOrdered == 0 );
            IndexOrdered = idx;
            _dRequires = requiresFromSorter.Select( s => (MutableItem)s.Item ).ToReadOnlyList();
            _dChildren = childrenFromSorter.Select( s => (MutableItem)s.Item ).ToReadOnlyList();
            _dGroups = groupsFromSorter.Select( s => (MutableItem)s.Item ).ToReadOnlyList();
            // requiredBy are useless.
            _dRequiredBy = null;
        }

        /// <summary>
        /// This is the natural index to reference a IStObj in a setup phasis.
        /// </summary>
        public int IndexOrdered { get; private set; }

        internal void CallConstruct( IActivityLogger logger, IStObjValueResolver dependencyResolver )
        {
            Debug.Assert( _constructParameterEx != null, "Always allocated." );

            if( AmbientTypeInfo.Construct == null ) return;

            object[] parameters = new object[_constructParameterEx.Count];
            int i = 0;
            foreach( MutableParameter t in _constructParameterEx )
            {
                // We inject our "setup logger" only if it is exactly the formal parameter: ... , IActivityLogger logger, ...
                // This enforces code homogeneity and let room for any other IActivityLogger injection.
                if( t.IsSetupLogger )
                {
                    t.SetParameterValue( logger );
                }
                else
                {
                    if( !t.HasBeenSet )
                    {
                        // Parameter reference have already been resolved as dependencies for graph construction since 
                        // no Value has been explicitely set for the parameter.
                        MutableItem resolved = t.CachedResolvedStObj;
                        if( resolved != null )
                        {
                            Debug.Assert( resolved.Object != Type.Missing );
                            t.SetParameterValue( resolved.Object );
                        }
                        else if( !t.IsRealParameterOptional )
                        {
                            if( !t.IsOptional )
                            {
                                // By throwing an exception here, we stop the process and avoid the construction 
                                // of an invalid object graph...
                                // This behavior (FailFastOnFailureToResolve) may be an option once. For the moment: log the error.
                                logger.Fatal( "{0}: Unable to resolve non optional. Attempting to use a default value to continue the setup process in order to detect other errors.", t.ToString() );
                            }
                            t.SetParameterValue( t.Type.IsValueType ? Activator.CreateInstance( t.Type ) : null );
                        }
                    }
                    if( dependencyResolver != null ) dependencyResolver.ResolveParameterValue( logger, t );
                }
                parameters[i++] = t.Value;
            }
            AmbientTypeInfo.Construct.Invoke( _leafData.StructuredObject, parameters );
        }

        #region IDependentItemContainerAsk Members

        string IDependentItem.FullName
        {
            get { return _dFullName; }
        }

        IDependentItemRef IDependentItem.Generalization
        {
            get { return _generalization; }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return _dContainer; }
        }

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get
            {
                IEnumerable<IDependentItemRef> r = _dChildren;
                if( _trackAmbientPropertiesMode == TrackAmbientPropertiesMode.AddPropertyHolderAsChildren )
                {
                    Debug.Assert( _trackedAmbientProperties != null );
                    var t = _trackedAmbientProperties.Select( a => a.Owner );
                    r = r != null ? r.Concat( r ) : t;
                }
                return r;
            }
        }

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups
        {
            get
            {
                IEnumerable<IDependentItemGroupRef> r = _dGroups;
                if( _trackAmbientPropertiesMode == TrackAmbientPropertiesMode.AddThisToPropertyHolderItems )
                {
                    Debug.Assert( _trackedAmbientProperties != null );
                    var t = _trackedAmbientProperties.Select( a => a.Owner );
                    r = r != null ? r.Concat( r ) : t;
                }
                return r;
            }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get 
            {
                Debug.Assert( _dRequires != null, "Built from the HashSet in PrepareDependentItem." );
                IEnumerable<IDependentItemRef> r = _dRequires;
                if( _trackAmbientPropertiesMode == TrackAmbientPropertiesMode.PropertyHolderRequiresThis )
                {
                    Debug.Assert( _trackedAmbientProperties != null );
                    r = r.Concat( _trackedAmbientProperties.Select( a => a.Owner ) ); 
                }
                return r; 
            }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get 
            {
                IEnumerable<IDependentItemRef> r = _dRequiredBy;
                if( _trackAmbientPropertiesMode == TrackAmbientPropertiesMode.PropertyHolderRequiredByThis )
                {
                    Debug.Assert( _trackedAmbientProperties != null );
                    var t = _trackedAmbientProperties.Select( a => a.Owner );
                    r = r != null ? r.Concat( r ) : t;
                }
                return r;
            }
        }

        object IDependentItem.StartDependencySort()
        {
            return null;
        }

        string IDependentItemRef.FullName
        {
            get { return _dFullName; }
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        #endregion

        #region IStObj Members

        public object Object
        {
            get { return _leafData.StructuredObject; }
        }

        public Type ObjectType
        {
            get { return Type; }
        }

        public new string Context
        {
            get { return base.Context; }
        }

        public DependentItemKind ItemKind 
        {
            get { return _itemKind; } 
        }

        IStObj IStObj.Generalization
        {
            get { return _generalization; }
        }

        IStObj IStObj.Specialization
        {
            get { return Specialization; }
        }

        IStObj IStObj.RootGeneralization
        {
            get { return _leafData.RootGeneralization; }
        }

        IStObj IStObj.LeafSpecialization
        {
            get { return _leafData.LeafSpecialization; }
        }

        IStObj IStObj.ConfiguredContainer 
        {
            get { return IsOwnContainer ? _dContainer : null; } 
        }

        IStObj IStObj.Container 
        { 
            get { return _dContainer; } 
        }

        IReadOnlyList<IStObj> IStObj.Requires 
        {
            get { return _dRequires; } 
        }

        IReadOnlyList<IStObj> IStObj.Children
        {
            get { return _dChildren; }
        }

        IReadOnlyList<IStObj> IStObj.Groups
        {
            get { return _dGroups; }
        }


        IReadOnlyList<IStObjTrackedAmbientPropertyInfo> IStObj.TrackedAmbientProperties
        {
            get 
            { 
                if( _trackedAmbientProperties == null ) return null;
                return _trackedAmbientPropertiesEx ?? (_trackedAmbientPropertiesEx = new ReadOnlyListOnIList<TrackedAmbientPropertyInfo>( _trackedAmbientProperties )); 
            }
        }

        object IStObj.GetStObjProperty( string propertyName )
        {
            StObjProperty p = GetStObjProperty( propertyName );
            return p != null ? p.Value : Type.Missing;
        }

        #endregion

    }
}
