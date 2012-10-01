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
    internal class MutableItem : IStObj, IStObjMutableItem, IDependentItemContainerAsk, IDependentItemContainerRef
    {
        readonly StObjTypeInfo _objectType;
        readonly Type _context;
        readonly object _stObj;
        readonly MutableItem _generalization;
        MutableItem _specialization;
        MutableItem _rootGeneralization;
        MutableItem _leafSpecialization;

        MutableReference _container;
        List<MutableReference> _requires;
        IReadOnlyList<MutableReference> _requiresEx;
        List<MutableReference> _requiredBy;
        IReadOnlyList<MutableReference> _requiredByEx;

        IReadOnlyList<MutableParameter> _constructParameterEx;
        IReadOnlyList<MutableAmbientProperty> _allAmbientProperties;

        string _dFullName;
        MutableItem _dContainer;
        IReadOnlyList<MutableItem> _dRequires;
        IReadOnlyList<MutableItem> _dRequiredBy;
        bool _hasBeenReferencedAsAContainer;
        byte _ambientPropertiesResolved;

        internal MutableItem( MutableItem parent, Type context, StObjTypeInfo objectType, object theObject )
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
            return AmbientContractCollector.DisplayName( _context, _objectType.Type );
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
        }

        void ApplyTypeInformation( IActivityLogger logger )
        {
            Debug.Assert( _container == null, "Called only once right after object instanciation." );
            Debug.Assert( _specialization == null || _specialization._rootGeneralization != null, "Configuration is from bottom to top." );

            _container = new MutableReference( this, MutableReferenceKind.Container );
            _container.Type = _objectType.Container;
            _container.Context = _objectType.ContainerContext;

            // We share Ambient properties from the Specialization.
            // This is why Configuration must be made from bottom to the top.
            if( _specialization == null )
            {
                _allAmbientProperties = _objectType.AmbientProperties.Select( ap => new MutableAmbientProperty( this, ap ) ).ToReadOnlyList();
            }
            else
            {
                _allAmbientProperties = _specialization._allAmbientProperties;
            }
            var a = _objectType.StObjAttribute;
            if( a != null )
            {
                if( a.Requires != null )
                {
                    _requires = a.Requires.Select( t => new MutableReference( this, MutableReferenceKind.Requires ) { Type = t, Context = _objectType.FindContextFromMapAttributes( t ) } ).ToList();
                }
                else _requires = new List<MutableReference>();
                if( a.RequiredBy != null )
                {
                    _requiredBy = a.RequiredBy.Select( t => new MutableReference( this, MutableReferenceKind.RequiredBy ) { Type = t, Context = _objectType.FindContextFromMapAttributes( t ) } ).ToList();
                }
                else _requiredBy = new List<MutableReference>();
            }
            else
            {
                _requires = new List<MutableReference>();
                _requiredBy = new List<MutableReference>();
            }
            _requiresEx = new ReadOnlyListOnIList<MutableReference>( _requires );
            _requiredByEx = new ReadOnlyListOnIList<MutableReference>( _requiredBy );
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

        IMutableReference IStObjMutableItem.Container { get { return _container; } }

        IReadOnlyList<IMutableReference> IStObjMutableItem.Requires { get { return _requiresEx; } }

        IReadOnlyList<IMutableReference> IStObjMutableItem.RequiredBy { get { return _requiredByEx; } }

        IReadOnlyList<IMutableParameter> IStObjMutableItem.ConstructParameters { get { return _constructParameterEx; } }

        IReadOnlyList<IMutableAmbientProperty> IStObjMutableItem.AllAmbientProperties { get { return _allAmbientProperties; } }

        Dictionary<PropertyInfo,object> _directPropertiesToSet;

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

        internal void PrepareDependentItem( IActivityLogger logger, StObjCollectorResult result, StObjCollectorContextualResult contextResult )
        {
            Debug.Assert( _container != null && _constructParameterEx != null );
            Debug.Assert( _context == contextResult.Context && result[_context] == contextResult, "We are called inside our typed context, this avoids the lookup result[Context] to obtain the owner's context (the default)." );

            _dFullName = AmbientContractCollector.DisplayName( _context, _objectType.Type );
            _dContainer = _container.ResolveToStObj( logger, result, contextResult );
            if( _dContainer != null )
            {
                // This is an optimization: handle containers only where it is necessary.
                _dContainer._hasBeenReferencedAsAContainer = true;
            }
            // Requirement intialization.
            HashSet<MutableItem> req = new HashSet<MutableItem>();
            {
                // Requires are... Required (when not configured as optional by IStObjStructuralConfigurator).
                foreach( MutableItem dep in _requires.Select( r => r.ResolveToStObj( logger, result, contextResult ) ).Where( m => m != null ) )
                {
                    req.Add( dep );
                }
                // Construct parameters are Required... except if they are one of our Container but this is handled
                // at the DependencySorter level by using the SkipDependencyToContainer option.
                // See the commented old code (to be kept) below for more detail on this option.
                if( _constructParameterEx.Count > 0 )
                {
                    foreach( MutableParameter t in _constructParameterEx )
                    {
                        MutableItem dep = t.ResolveToStObj( logger, result, contextResult );
                        if( dep != null ) req.Add( dep );
                    }
                }
            }
            // This will be updated after the Sort with clean Requirements (no Generalization nor Containers in it).
            _dRequires = req.ToReadOnlyList();

            // RequiredBy initialization.
            if( _requiredBy.Count > 0 )
            {
                _dRequiredBy = _requiredBy.Select( r => r.ResolveToStObj( logger, result, contextResult ) ).Where( m => m != null ).ToReadOnlyList();
            }
        }

        #region PrepareDependentItem: before sorting (old fully commented code to be kept for documentation - SkipDependencyToContainer option rational).
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
        /// <param name="containerFromSorter">Container (with Generalization's inheritance).</param>
        /// <param name="requiresFromSorter">Cleaned up requirements (no Genralization nor Containers).</param>
        internal void SetSorterData( int idx, ISortedItem containerFromSorter, IEnumerable<IDependentItemRef> requiresFromSorter )
        {
            Debug.Assert( IndexOrdered == 0 );
            Debug.Assert( _dContainer == null || _dContainer == containerFromSorter.Item );
            IndexOrdered = idx;
            if( _dContainer == null && containerFromSorter != null )
            {
                _dContainer = (MutableItem)containerFromSorter.Item;
            }
            _dRequires = requiresFromSorter.Cast<MutableItem>().ToReadOnlyList();
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
        
        internal void EnsureAmbientPropertiesResolved( IActivityLogger logger, StObjCollectorResult result, IStObjDependencyResolver dependencyResolver )
        {
            if( _ambientPropertiesResolved == 1 ) return;
            if( _specialization != null )
            {
                _specialization.EnsureAmbientPropertiesResolved( logger, result, dependencyResolver );
                _ambientPropertiesResolved = 1;
                return;
            }
            Debug.Assert( _specialization == null && _leafSpecialization == this, "We are on the specialization." );
            if( _ambientPropertiesResolved == 2 ) throw new CKException( "Recursivity in AmbientProperties resolution. Please contact the developer :-(." );
            _ambientPropertiesResolved = 2;
            try
            {
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
                    if( dependencyResolver != null ) dependencyResolver.ResolvePropertyValue( logger, a );
                    if( a.Value == Type.Missing )
                    {
                        MutableItem resolved = a.ResolveToStObj( logger, result, null );
                        if( resolved != null )
                        {
                            Debug.Assert( resolved.Object != Type.Missing ); 
                            a.SetResolvedValue( logger, resolved.Object );
                        }
                        else 
                        {
                            // Let's try to locate the property.
                            IAmbientPropertyGetter getter = LocateAmbientProperty( logger, result, dependencyResolver, a.Type, a.Name );
                            if( getter != null )
                            {
                                a.SetResolvedValue( logger, getter.GetValue() );
                            }
                        }
                    }
                    if( a.Value != Type.Missing )
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
                            object current = null;
                            object value = a.Value;
                            if( a.IsMergeable )
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
                            if( current == null ) a.PropertyInfo.SetValue( _stObj, a.Value, null );
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
            finally
            {
                _ambientPropertiesResolved = 1;
            }
        }

        interface IAmbientPropertyGetter
        {
            object GetValue();
        }

        class AmbientPropertyGetterFromRealProperty : IAmbientPropertyGetter
        {
            MutableItem _holder;
            PropertyInfo _realProperty;

            public AmbientPropertyGetterFromRealProperty( MutableItem holder, PropertyInfo realProperty )
            {
                _holder = holder;
                _realProperty = realProperty;
            }

            public object GetValue()
            {
                return _realProperty.GetValue( _holder.Object, null );
            }
        }

        IAmbientPropertyGetter LocateAmbientProperty( IActivityLogger logger, StObjCollectorResult result, IStObjDependencyResolver dependencyResolver, Type propertyType, string name )
        {
            IAmbientPropertyGetter getter = null;
            MutableItem start = this;
            while( start != null && getter == null )
            {
                if( start._dContainer != null )
                {
                    start._dContainer.EnsureAmbientPropertiesResolved( logger, result, dependencyResolver );
                    getter = start._dContainer.FindAmbientProperty( logger, propertyType, name );
                }
                start = start._generalization;
            }
            return getter;
        }

        IAmbientPropertyGetter FindAmbientProperty( IActivityLogger logger, Type propertyType, string name )
        {
            var exist = _allAmbientProperties.FirstOrDefault( a => a.Name == name );
            if( exist == null ) return null;
            // A property exists at the Specialization level, but does it exist for this slice?
            if( !exist.IsDefinedFor( _objectType.Type ) ) return null;
            // Type compatible ?
            if( !propertyType.IsAssignableFrom( exist.Type ) )
            {
                Debug.Assert( exist.Owner == this );
                logger.Warn( "Looking for property named '{0}' of type '{1}': found a candidate on '{2}' but type does not match (it is '{3}'). It is ignored.", name, propertyType.Name, ToString(), exist.Type.Name );
            }
            return new AmbientPropertyGetterFromRealProperty( exist.Owner, exist.PropertyInfo );
        }

        #region IDependentItemContainerAsk Members

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return null; }
        }

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

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get { return _dRequires; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get { return _dRequiredBy; }
        }

        bool IDependentItemContainerAsk.ThisIsNotAContainer
        {
            get { return !_hasBeenReferencedAsAContainer; }
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

        public Type Context
        {
            get { return _context; }
        }

        public bool IsContainer
        {
            get { return _hasBeenReferencedAsAContainer; }
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

        #endregion

    }
}
