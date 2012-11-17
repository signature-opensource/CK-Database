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
    internal class MutableItem : IStObj, IStObjMutableItem, IDependentItemContainerTyped, IDependentItemContainerRef
    {
        readonly StObjTypeInfo _objectType;
        readonly string _context;
        readonly object _stObj;
        readonly MutableItem _generalization;
        MutableItem _specialization;
        MutableItem _rootGeneralization;
        MutableItem _leafSpecialization;

        MutableReference _container;
        MutableReferenceList _requires;
        MutableReferenceList _requiredBy;
        MutableReferenceList _children;
        MutableReferenceList _groups;
        
        IReadOnlyList<MutableParameter> _constructParameterEx;
        DependentItemType _itemKind;
        /// <summary>
        /// Ambient Properties are shared by the inheritance chain (it is
        /// initialized once at the specialization level).
        /// </summary>
        IReadOnlyList<MutableAmbientProperty> _allAmbientProperties;
        
        string _dFullName;
        MutableItem _dContainer;
        IReadOnlyList<MutableItem> _dRequires;
        IReadOnlyList<MutableItem> _dRequiredBy;
        IReadOnlyList<MutableItem> _dChildren;
        IReadOnlyList<MutableItem> _dGroups;

        Dictionary<PropertyInfo,object> _directPropertiesToSet;
        TrackAmbientPropertiesMode _trackAmbientPropertiesMode;
        List<TrackedAmbientPropertyInfo> _trackedAmbientProperties;
        IReadOnlyList<TrackedAmbientPropertyInfo> _trackedAmbientPropertiesEx;

        enum PrepareState : byte
        {
            None,
            RecursePreparing,
            Done
        }
        PrepareState _prepareState;

        internal MutableItem( MutableItem parent, string context, StObjTypeInfo objectType, object theObject )
        {
            Debug.Assert( context != null  && theObject != null );
            _objectType = objectType;
            _stObj = theObject;
            _context = context;
            _generalization = parent;
            if( _generalization != null )
            {
                Debug.Assert( _generalization._specialization == null );
                _generalization._specialization = this;
            }
        }

        public override string ToString()
        {
            return ContextNaming.FormatContextPrefix( _objectType.Type.FullName, _context );
        }

        #region Configuration

        internal void Configure( IActivityLogger logger, MutableItem rootGeneralization, MutableItem leafSpecialization )
        {
            Debug.Assert( _rootGeneralization == null && _leafSpecialization == null, "Configured once and only once." );
            Debug.Assert( rootGeneralization != null && leafSpecialization != null, "Configuration sets the top & bottom of the inheritance chain." );
            Debug.Assert( (rootGeneralization == this) == (_generalization == null) );
            Debug.Assert( (leafSpecialization == this) == (_specialization == null) );

            _rootGeneralization = rootGeneralization;
            _leafSpecialization = leafSpecialization;
            ApplyTypeInformation( logger );
            AnalyseConstruct( logger );
            ApplyConfiguratorAttributes( logger );
            // Since TrackAmbientProperties is inherited, we only know its actual value once PrepareDependentItem has done its job.
            // If other items have Ambient Properties that reference this item, they have to register themselves in our _trackedAmbientProperties 
            // even if our own PrepareDependentItem has not been called yet.
            // This is why we always allocate the list EXCEPT if the mode is None... When Unknown, we keep it open.
            // (And no, when registering an Ambient Property, calling PrepareDependentItem is not an option since that would lead to erroneous cycle detection!)
            if( _trackAmbientPropertiesMode != TrackAmbientPropertiesMode.None ) _trackedAmbientProperties = new List<TrackedAmbientPropertyInfo>();
        }

        void ApplyTypeInformation( IActivityLogger logger )
        {
            Debug.Assert( _container == null, "Called only once right after object instanciation." );
            Debug.Assert( _specialization == null || _specialization._rootGeneralization != null, "Configuration is from bottom to top." );

            _container = new MutableReference( this, MutableReferenceKind.Container );
            _container.Type = _objectType.Container;
            _container.Context = _objectType.ContainerContext;
            _itemKind = _objectType.ItemKind;
            // Use TrackAmbientProperties setter to allocate _trackedAmbientProperties.
            TrackAmbientProperties = _objectType.TrackAmbientProperties;
            // We share Ambient properties from the Specialization.
            // This is why Configuration must be made from leaf to root.
            if( _specialization == null )
            {
                _allAmbientProperties = _objectType.AmbientProperties.Select( ap => new MutableAmbientProperty( this, ap ) ).ToReadOnlyList();
            }
            else
            {
                _allAmbientProperties = _specialization._allAmbientProperties;
            }

            _requires = new MutableReferenceList( this, MutableReferenceKind.Requires );
            if( _objectType.Requires != null )
            {
                _requires.AddRange( _objectType.Requires.Select( t => new MutableReference( this, MutableReferenceKind.Requires ) { Type = t, Context = _objectType.FindContextFromMapAttributes( t ) } ) );
            }
            _requiredBy = new MutableReferenceList( this, MutableReferenceKind.RequiredBy );           
            if( _objectType.RequiredBy != null )
            {
                _requiredBy.AddRange( _objectType.RequiredBy.Select( t => new MutableReference( this, MutableReferenceKind.RequiredBy ) { Type = t, Context = _objectType.FindContextFromMapAttributes( t ) } ) );
            }
            _children = new MutableReferenceList( this, MutableReferenceKind.Child );
            if( _objectType.Children != null )
            {
                _children.AddRange( _objectType.Children.Select( t => new MutableReference( this, MutableReferenceKind.RequiredBy ) { Type = t, Context = _objectType.FindContextFromMapAttributes( t ) } ) );
            }
            _groups = new MutableReferenceList( this, MutableReferenceKind.Group );
            if( _objectType.Groups != null )
            {
                _groups.AddRange( _objectType.Groups.Select( t => new MutableReference( this, MutableReferenceKind.Group ) { Type = t, Context = _objectType.FindContextFromMapAttributes( t ) } ) );
            }
        }

        void AnalyseConstruct( IActivityLogger logger )
        {
            Debug.Assert( _constructParameterEx == null, "Called only once right after object instanciation..." );
            Debug.Assert( _container != null, "...and after ApplyTypeInformation." );

            if( _objectType.Construct != null && _objectType.ConstructParameters.Length > 0 )
            {
                var parameters = new MutableParameter[ _objectType.ConstructParameters.Length ];
                for( int idx = 0; idx < parameters.Length; ++idx )
                {
                    ParameterInfo cp = _objectType.ConstructParameters[idx];
                    bool isContainer = idx == _objectType.ContainerConstructParameterIndex;
                    MutableParameter p = new MutableParameter( this, cp, isContainer );
                    p.Context = _objectType.ConstructParameterTypedContext[idx];
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

        void ApplyConfiguratorAttributes( IActivityLogger logger )
        {
            foreach( var c in _objectType.ConfiguratorAttributes )
            {
                c.Configure( logger, this );
            }
        }

        #endregion

        internal MutableItem Generalization
        {
            get { return _generalization; }
        }

        internal MutableItem Specialization
        {
            get { return _specialization; }
        }

        #region IStObjMutableItem is called during Configuration

        DependentItemType IStObjMutableItem.ItemKind
        {
            get { return _itemKind; }
            set { _itemKind = value; }
        }
        
        public TrackAmbientPropertiesMode TrackAmbientProperties
        {
            get { return _trackAmbientPropertiesMode; }
            set { _trackAmbientPropertiesMode = value; }
        }

        IMutableReference IStObjMutableItem.Container { get { return _container; } }

        IMutableReferenceList IStObjMutableItem.Children { get { return _children; } }

        IMutableReferenceList IStObjMutableItem.Requires { get { return _requires; } }

        IMutableReferenceList IStObjMutableItem.RequiredBy { get { return _requiredBy; } }

        IMutableReferenceList IStObjMutableItem.Groups { get { return _groups; } }

        IReadOnlyList<IMutableParameter> IStObjMutableItem.ConstructParameters { get { return _constructParameterEx; } }

        IReadOnlyList<IMutableAmbientProperty> IStObjMutableItem.AllAmbientProperties { get { return _allAmbientProperties; } }

        bool IStObjMutableItem.SetPropertyStructuralValue( IActivityLogger logger, string sourceName, string propertyName, object value )
        {
            if( value == Type.Missing ) throw new InvalidOperationException( "Setting property to Type.Missing is not allowed." );

            MutableAmbientProperty mp = _allAmbientProperties.FirstOrDefault( a => a.Name == propertyName );
            if( mp != null )
            {
                return mp.SetStructuralValue( logger, sourceName, value );
            }

            // Targets the specialization to honor property covariance.
            PropertyInfo p = _leafSpecialization._objectType.Type.GetProperty( propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null, Type.EmptyTypes, null );           
            if( p == null || !p.CanWrite )
            {
                logger.Error( "Unable to set property '{1}.{0}' structural value. It must exist and be writeable.", propertyName, _leafSpecialization._objectType.Type.FullName );
                return false;
            }
            if( _leafSpecialization._directPropertiesToSet == null ) _leafSpecialization._directPropertiesToSet = new Dictionary<PropertyInfo, object>();
            _leafSpecialization._directPropertiesToSet[p] = value;
            return true;
        }
       
        #endregion

        internal bool PrepareDependendtItem( IActivityLogger logger, IStObjDependencyResolver dependencyResolver, StObjCollectorResult collector, StObjCollectorContextualResult cachedCollector )
        {
            if( _prepareState == PrepareState.Done ) return true;
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
                            if( _itemKind == DependentItemType.Unknown ) _itemKind = _generalization._itemKind;
                            // Use the TrackAmbientProperties property setter to trigger _trackAmbientProperties allocation.
                            if( _trackAmbientPropertiesMode == TrackAmbientPropertiesMode.Unknown ) TrackAmbientProperties = _generalization._trackAmbientPropertiesMode;
                        }
                        // Check configuration.
                        if( _itemKind == DependentItemType.Unknown )
                        {
                            logger.Warn( "ItemKind is not specified. It defaults to SimpleItem. It should be set explicitely to either SimpleItem, Group or Container." );
                            _itemKind = DependentItemType.SimpleItem;
                        }
                        if( _trackAmbientPropertiesMode == TrackAmbientPropertiesMode.Unknown ) TrackAmbientProperties = TrackAmbientPropertiesMode.None;
                        
                        // Closes Ambient Properties now that we know the final configuration for it.
                        Debug.Assert( _trackAmbientPropertiesMode != TrackAmbientPropertiesMode.Unknown );
                        if( _trackAmbientPropertiesMode == TrackAmbientPropertiesMode.None )
                        {
                            _trackedAmbientProperties = null;
                        }
                        
                        // If we are on a Specialization, we can set the Ambient Properties since
                        // the Container and the Generalization have been prepared, properties can safely 
                        // be located and propagated to this StObj.
                        if( _specialization == null )
                        {
                            LocateAndSetAmbientPropertiesOnSpecialization( logger, collector, dependencyResolver );
                        }
                    }
                    return result;

                }
                finally
                {
                    _prepareState = PrepareState.Done;
                }
            }
        }

        bool ResolveDirectReferences( IActivityLogger logger, StObjCollectorResult collector, StObjCollectorContextualResult cachedCollector )
        {
            Debug.Assert( _container != null && _constructParameterEx != null );
            bool result = true;
            _dFullName = ContextNaming.FormatContextPrefix( _objectType.Type.FullName, _context );
            _dContainer = _container.ResolveToStObj( logger, collector, cachedCollector );
            // Requirement intialization.
            HashSet<MutableItem> req = new HashSet<MutableItem>();
            {
                // Requires are... Required (when not configured as optional by IStObjStructuralConfigurator).
                foreach( MutableItem dep in _requires.AsList.Select( r => r.ResolveToStObj( logger, collector, cachedCollector ) ) )
                {
                    if( dep != null ) req.Add( dep );
                }
                // Construct parameters are Required... except if they are one of our Container but this is handled
                // at the DependencySorter level by using the SkipDependencyToContainer option.
                // See the commented old code (to be kept) below for more detail on this option.
                if( _constructParameterEx.Count > 0 )
                {
                    foreach( MutableParameter t in _constructParameterEx )
                    {
                        MutableItem dep = t.ResolveToStObj( logger, collector, cachedCollector );
                        if( dep != null ) req.Add( dep );
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

        #region Ambient Properties
        void LocateAndSetAmbientPropertiesOnSpecialization( IActivityLogger logger, StObjCollectorResult result, IStObjDependencyResolver dependencyResolver )
        {
            Debug.Assert( _specialization == null && _leafSpecialization == this, "We are on the specialization." );
            Debug.Assert( _prepareState == PrepareState.RecursePreparing );
            if( _directPropertiesToSet != null )
            {
                foreach( var k in _directPropertiesToSet )
                {
                    try
                    {
                        if( k.Value != Type.Missing ) k.Key.SetValue( _stObj, k.Value, null );
                    }
                    catch( Exception ex )
                    {
                        logger.Error( ex, "While setting property '{1}.{0}'.", k.Key.Name, k.Key.DeclaringType.FullName );
                    }
                }
            }
            foreach( var a in _allAmbientProperties )
            {
                MutableItem directResolved = a.ResolveToStObj( logger, result, null );
                if( directResolved != null )
                {
                    Debug.Assert( directResolved.Object != Type.Missing );
                    // Trick: to be able to track Ambient Properties we need the MutableItem that corresponds to the property
                    // value. To avoid a global dictionary object => MutableItem (Specialization) we store the MutableItem 
                    // as the Resolved value (we extract its Object to actually set the property value below).
                    // Because of Type compatibility checks that are done, we can not have here a generalisation but we may have a specialization...
                    // ...a lookup in the generalization chain to locate the best abstraction according to this property type is needed.
                    a.SetResolvedValue( logger, directResolved );
                }
                else
                {
                    // Let's try to locate the property.
                    MutableAmbientProperty getter = LocateAmbientProperty( logger, result, dependencyResolver, a.Type, a.Name );
                    if( getter != null )
                    {
                        // See above: the associated Value may be a MutableItem.
                        a.SetResolvedValue( logger, getter.Value );
                    }
                }
                if( a.Value == Type.Missing )
                {
                    if( dependencyResolver != null ) dependencyResolver.ResolveExternalPropertyValue( logger, a );
                }
                object value = a.Value;
                if( value != Type.Missing )
                {
                    // Actual ambient property setting.
                    //
                    // Current model does not enable the IStObjDependencyResolver to merge/combine
                    // the current value and the resolved value. 
                    // Nor does it enable a post-initialization step to occur.
                    // for complex properties this may be useful: to support such feature one will need
                    // to give IStObjDependencyResolver.ResolvePropertyValue more information (the IAmbientPropertyGetter) and 
                    // more control on setting vs. merging the value.
                    // The current implementation seems enough but this could be changed once actually needed.
                    try
                    {
                        MutableItem resolved = directResolved;
                        if( resolved == null ) resolved = value as MutableItem;
                        // If the property value is a StObj, extracts its value.
                        if( resolved != null ) value = resolved.Object;

                        object current = null;
                        if( a.IsValueMergeable )
                        {
                            current = a.PropertyInfo.GetValue( _stObj, null );
                            if( current != null )
                            {
                                if( !((IMergeable)current).Merge( value, new SimpleServiceContainer().Add( logger ) ) )
                                {
                                    logger.Error( "Unable to merge ambient property '{1}.{0}'.", a.Name, a.PropertyInfo.DeclaringType.FullName );
                                }
                            }
                        }
                        if( current == null )
                        {
                            a.PropertyInfo.SetValue( _stObj, value, null );
                            if( resolved != null && resolved._trackedAmbientProperties != null )
                            {
                                if( directResolved == null )
                                {
                                    // Walks up the chain to locate the most abstract compatible slice.
                                    MutableItem gen = resolved.Generalization;
                                    while( gen != null && a.Type.IsAssignableFrom( gen.ObjectType ) )
                                    {
                                        resolved = gen;
                                        gen = gen.Generalization;
                                    }
                                }
                                resolved._trackedAmbientProperties.Add( new TrackedAmbientPropertyInfo( a.Owner, a.PropertyInfo ) );
                            }
                        }
                    }
                    catch( Exception ex )
                    {
                        logger.Error( ex, "While setting ambient property '{1}.{0}'.", a.Name, a.PropertyInfo.DeclaringType.FullName );
                    }
                }
                else if( !a.IsOptional )
                {
                    logger.Error( "{0}: Unable to resolve non optional.", a.ToString() );
                }
            }
        }

        MutableAmbientProperty LocateAmbientProperty( IActivityLogger logger, StObjCollectorResult result, IStObjDependencyResolver dependencyResolver, Type propertyType, string name )
        {
            MutableAmbientProperty getter = null;
            MutableItem start = this;
            while( start != null && getter == null )
            {
                if( start._dContainer != null )
                {
                    getter = start._dContainer.FindAmbientProperty( logger, propertyType, name );
                }
                start = start._generalization;
            }
            return getter;
        }

        MutableAmbientProperty FindAmbientProperty( IActivityLogger logger, Type propertyType, string name )
        {
            MutableAmbientProperty exist = _allAmbientProperties.FirstOrDefault( a => a.Name == name );
            if( exist == null ) return null;
            // A property exists at the Specialization level, but does it exist for this slice?
            if( !exist.IsDefinedFor( _objectType.Type ) ) return null;
            // Type compatible ?
            if( !propertyType.IsAssignableFrom( exist.Type ) )
            {
                Debug.Assert( exist.Owner == this );
                logger.Warn( "Looking for property named '{0}' of type '{1}': found a candidate on '{2}' but type does not match (it is '{3}'). It is ignored.", name, propertyType.Name, ToString(), exist.Type.Name );
            }
            return exist;
        }
        #endregion

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

        internal void CallConstruct( IActivityLogger logger, IStObjDependencyResolver dependencyResolver )
        {
            Debug.Assert( _constructParameterEx != null, "Always allocated." );
            
            if( _objectType.Construct == null ) return;

            object[] parameters = new object[_constructParameterEx.Count];
            int i = 0;
            foreach( MutableParameter t in _constructParameterEx )
            {
                // We inject our "setup logger" only if it is exactly the formal parameter: ... , IActivityLogger logger, ...
                // This enforces code homogeneity and let room for any other IActivityLogger injection.
                if( t.IsSetupLogger )
                {
                    t.SetResolvedValue( logger, logger );
                }
                else
                {
                    if( dependencyResolver != null ) dependencyResolver.ResolveParameterValue( logger, t );
                    if( t.Value == Type.Missing )
                    {
                        // Parameter reference have already been resolved as dependencies for graph construction.
                        MutableItem resolved = t.CachedResolvedStObj;
                        if( resolved != null )
                        {
                            Debug.Assert( resolved.Object != Type.Missing );
                            t.SetResolvedValue( logger, resolved.Object );
                        }
                        else if( !t.IsRealParameterOptional )
                        {
                            if( t.IsOptional )
                            {
                                t.SetResolvedValue( logger, t.Type.IsValueType ? Activator.CreateInstance( t.Type ) : null );
                            }
                            else
                            {
                                // By throwing an exception here, we stop the process and avoid the construction 
                                // of an invalid object graph...
                                // This behavior (FailFastOnFailureToResolve) may be an option once. For the moment: log the error.
                                logger.Fatal( "{0}: Unable to resolve non optional. Attempting to use a default value to continue the setup process in order to detect other errors.", t.ToString() );
                                t.SetResolvedValue( logger, t.Type.IsValueType ? Activator.CreateInstance( t.Type ) : null );
                            }
                        }
                    }
                }
                parameters[i++] = t.Value;
            }
            _objectType.Construct.Invoke( _stObj, parameters );
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
                    var t = _trackedAmbientProperties.Select( a => a.SpecializedOwner );
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
                    var t = _trackedAmbientProperties.Select( a => a.SpecializedOwner );
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
                    r = r.Concat( _trackedAmbientProperties.Select( a => a.SpecializedOwner ) ); 
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
                    var t = _trackedAmbientProperties.Select( a => a.SpecializedOwner );
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
            get { return _stObj; }
        }

        public Type ObjectType
        {
            get { return _objectType.Type; }
        }

        public string Context
        {
            get { return _context; }
        }

        public DependentItemType ItemKind 
        {
            get { return _itemKind; } 
        }

        IStObj IStObj.Generalization
        {
            get { return _generalization; }
        }

        IStObj IStObj.Specialization
        {
            get { return _specialization; }
        }

        IStObj IStObj.RootGeneralization
        {
            get { return _rootGeneralization; }
        }

        IStObj IStObj.LeafSpecialization
        {
            get { return _leafSpecialization; }
        }

        IStObj IStObj.ConfiguredContainer 
        {
            // TODO: check this seriously.
            get { return _dContainer != null && _dContainer.ObjectType == _container.Type ? _dContainer : null; } 
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


        IReadOnlyList<ITrackedAmbientPropertyInfo> IStObj.TrackedAmbientProperties
        {
            get 
            { 
                if( _trackedAmbientProperties == null ) return null;
                return _trackedAmbientPropertiesEx ?? (_trackedAmbientPropertiesEx = new ReadOnlyListOnIList<TrackedAmbientPropertyInfo>( _trackedAmbientProperties )); 
            }
        }

        #endregion

    }
}
