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
        readonly MutableItem _directGeneralization;
        MutableItem _directSpecialization;
        MutableItem _generalization;
        MutableItem _specialization;

        MutableReference _container;
        List<MutableReference> _requires;
        IReadOnlyList<MutableReference> _requiresEx;
        List<MutableReference> _requiredBy;
        IReadOnlyList<MutableReference> _requiredByEx;

        IReadOnlyList<MutableParameter> _constructParameterEx;
        IReadOnlyList<MutableAmbiantProperty> _allAmbiantProperties;

        string _dFullName;
        MutableItem _dContainer;
        IReadOnlyList<MutableItem> _dRequires;
        IReadOnlyList<MutableItem> _dRequiredBy;
        bool _hasBeenReferencedAsAContainer;
        byte _ambiantPropertiesResolved;

        internal MutableItem( MutableItem parent, Type context, StObjTypeInfo objectType, object theObject )
        {
            Debug.Assert( context != null  && theObject != null );
            _objectType = objectType;
            _stObj = theObject;
            _context = context;
            _directGeneralization = parent;
            if( _directGeneralization != null )
            {
                Debug.Assert( _directGeneralization._directSpecialization == null );
                _directGeneralization._directSpecialization = this;
            }
        }

        public override string ToString()
        {
            return AmbiantContractCollector.DisplayName( _context, _objectType.Type );
        }

        #region Configuration

        internal void Configure( IActivityLogger logger, MutableItem generalization, MutableItem specialization )
        {
            Debug.Assert( _generalization == null && _specialization == null, "Configured once and only once." );
            Debug.Assert( generalization != null && specialization != null, "Configuration sets the top & bottom of the inheritance chain." );
            Debug.Assert( (generalization == this) == (_directGeneralization == null) );
            Debug.Assert( (specialization == this) == (_directSpecialization == null) );

            _generalization = generalization;
            _specialization = specialization;
            ApplyTypeInformation( logger );
            AnalyseConstruct( logger );
            ApplyConfiguratorAttributes( logger );
        }

        void ApplyTypeInformation( IActivityLogger logger )
        {
            Debug.Assert( _container == null, "Called only once right after object instanciation." );
            Debug.Assert( _directSpecialization == null || _directSpecialization._generalization != null, "Configuration is from bottom to top." );

            _container = new MutableReference( this, MutableReferenceKind.Container );
            _container.Type = _objectType.Container;
            _container.Context = _objectType.ContainerContext;

            // We share Ambiant properties from the Specialization.
            // This is why Configuration must be made from bottom to the top.
            if( _directSpecialization == null )
            {
                _allAmbiantProperties = _objectType.AmbiantProperties.Select( ap => new MutableAmbiantProperty( this, ap ) ).ToReadOnlyList();
            }
            else
            {
                _allAmbiantProperties = _directSpecialization._allAmbiantProperties;
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

        internal MutableItem DirectGeneralization
        {
            get { return _directGeneralization; }
        }

        internal MutableItem DirectSpecialization
        {
            get { return _directSpecialization; }
        }

        #region IStObjMutableItem is called during Configuration

        IMutableReference IStObjMutableItem.Container { get { return _container; } }

        IReadOnlyList<IMutableReference> IStObjMutableItem.Requires { get { return _requiresEx; } }

        IReadOnlyList<IMutableReference> IStObjMutableItem.RequiredBy { get { return _requiredByEx; } }

        IReadOnlyList<IMutableParameter> IStObjMutableItem.ConstructParameters { get { return _constructParameterEx; } }

        IReadOnlyList<IMutableAmbiantProperty> IStObjMutableItem.AllAmbiantProperties { get { return _allAmbiantProperties; } }

        bool IStObjMutableItem.SetPropertyStructuralValue( IActivityLogger logger, string sourceName, string propertyName, object value )
        {
            if( value == Type.Missing ) throw new InvalidOperationException( "Setting property to Type.Missing is not allowed." );

            // Targets the specialization to honor property covariance.
            PropertyInfo p;
            MutableAmbiantProperty mp = _specialization._allAmbiantProperties.FirstOrDefault( a => a.Name == propertyName );
            if( mp != null ) p = mp.PropertyInfo;
            else p = _specialization._objectType.Type.GetProperty( propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null, Type.EmptyTypes, null );
            
            if( p == null || !p.CanWrite )
            {
                logger.Error( "Unable to set property '{1}.{0}' structural value. It must exist and be writeable.", propertyName, _specialization._objectType.Type.FullName );
            }
            else 
            {
                try
                {
                    p.SetValue( _stObj, value, null );
                    if( mp != null ) mp.Value = value;
                    return true;
                }
                catch( Exception ex )
                {
                    logger.Error( ex, "While setting structural property '{1}.{0}'.", propertyName, _specialization._objectType.Type.FullName );
                }
            }
            return false;
        }

        #endregion

        #region PrepareDependentItem: before sorting.
        internal void PrepareDependentItem( IActivityLogger logger, StObjCollectorResult result, StObjCollectorContextualResult contextResult )
        {
            Debug.Assert( _container != null && _constructParameterEx != null );
            Debug.Assert( _context == contextResult.Context && result[_context] == contextResult, "We are called inside our typed context, this avoids the lookup result[Context] to obtain the owner's context (the default)." );

            // Container initialization.
            //
            // Since we want to remove all the containers of the object from its parameter requirements (see below), 
            // we can not rely on the DependencySorter to detect a cyclic chain of containers:
            // we use the list to collect the chain of containers and detect cycles.
            List<MutableItem> allContainers = null;
            ComputeFullNameAndResolveContainer( logger, result, contextResult, ref allContainers );

            // Requirement intialization.
            HashSet<MutableItem> req = new HashSet<MutableItem>();
            {
                // Base class is Required.
                if( _directGeneralization != null ) req.Add( _directGeneralization );

                // Requires are... Required (when not configured as optional by IStObjStructuralConfigurator).
                foreach( MutableItem dep in _requires.Select( r => r.ResolveToStObj( logger, result, contextResult ) ).Where( m => m != null ) )
                {
                    req.Add( dep );
                }
                // Construct parameters are Required... except if they are one of our Container.
                if( _constructParameterEx.Count > 0 )
                {
                    // We are here considering here that a Container parameter does NOT define a dependency to the whole container (with its content):
                    //
                    //      That seems strange: we may expect the container to be fully initialized when used as a parameter by a dependency Construct...
                    //      The fact is that we are dealing with Objects that have a method Construct, that this Construct method is called on the head
                    //      of the container (before any of its content) and that this method has no "thickness", no content in terms of dependencies: its
                    //      execution fully initializes the StOj and we can use it.
                    //      This is actually fully coherent with the way the setup works. An item of a package does not "require" its own package, it is 
                    //      contained in its package and can require items in the package as it needs.
                    // 
                    foreach( MutableParameter t in _constructParameterEx )
                    {
                        // Do not consider the container as a requirement since a Container is
                        // already a dependency (on the head's Container) and that a requirement on a container
                        // targets the whole content of it (this would lead to a cycle in the dependency graph).
                        MutableItem dep = t.ResolveToStObj( logger, result, contextResult );
                        if( dep != null && (allContainers == null || allContainers.Contains( dep ) == false) ) req.Add( dep );
                    }
                }
            }
            _dRequires = req.ToReadOnlyList();

            // RequiredBy initialization.
            if( _requiredBy.Count > 0 )
            {
                _dRequiredBy = _requiredBy.Select( r => r.ResolveToStObj( logger, result, contextResult ) ).Where( m => m != null ).ToReadOnlyList();
            }
        }

        void ComputeFullNameAndResolveContainer( IActivityLogger logger, StObjCollectorResult result, StObjCollectorContextualResult contextResult, ref List<MutableItem> prevContainers )
        {
            if( _dFullName != null ) return;

            _dFullName = AmbiantContractCollector.DisplayName( _context, _objectType.Type );
            _dContainer = _container.ResolveToStObj( logger, result, contextResult );
            if( _dContainer != null )
            {
                _dContainer._hasBeenReferencedAsAContainer = true;
                if( prevContainers == null ) prevContainers = new List<MutableItem>();
                else if( prevContainers.Contains( _dContainer ) )
                {
                    logger.Fatal( "Recursive Container chain encountered: '{0}'.", String.Join( "', '", prevContainers.Select( m => m._dFullName ) ) );
                    return;
                }
                prevContainers.Add( _dContainer );
                Type containerContext = _dContainer.Context;
                if( containerContext != contextResult.Context )
                {
                    contextResult = result[containerContext];
                    Debug.Assert( contextResult != null );
                }
                _dContainer.ComputeFullNameAndResolveContainer( logger, result, contextResult, ref prevContainers );
            }
        }
        #endregion

        /// <summary>
        /// The index is set by StObjCollector once the mutable items have been sorted.
        /// </summary>
        public int IndexOrdered { get; internal set; }

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
                            Debug.Assert( resolved.StructuredObject != Type.Missing );
                            t.SetResolvedValue( logger, resolved.StructuredObject );
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
                                // This behavior (FailFastOnFailureToResolve) may be an option once.
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

        
        internal void EnsureAmbiantPropertiesResolved( IActivityLogger logger, StObjCollectorResult result, IStObjDependencyResolver dependencyResolver )
        {
            if( _ambiantPropertiesResolved == 1 ) return;
            if( _directSpecialization != null )
            {
                _directSpecialization.EnsureAmbiantPropertiesResolved( logger, result, dependencyResolver );
                _ambiantPropertiesResolved = 1;
                return;
            }
            if( _ambiantPropertiesResolved == 2 ) throw new CKException( "Recursivity in AmbiantProperties resolution. Please contact the developer :-(." );
            _ambiantPropertiesResolved = 2;
            try
            {
                foreach( var a in _allAmbiantProperties )
                {
                    if( dependencyResolver != null ) dependencyResolver.ResolvePropertyValue( logger, a );
                    if( a.Value == Type.Missing )
                    {
                        MutableItem resolved = a.ResolveToStObj( logger, result, null );
                        if( resolved != null )
                        {
                            Debug.Assert( resolved.StructuredObject != Type.Missing ); 
                            a.SetResolvedValue( logger, resolved.StructuredObject );
                        }
                        else 
                        {
                            // Let's try to locate the property.
                            IAmbiantPropertyGetter getter = LocateAmbiantProperty( logger, result, dependencyResolver, a.Type, a.Name );
                            if( getter != null )
                            {
                                a.SetResolvedValue( logger, getter.GetValue() );
                            }
                        }
                    }
                    if( a.Value != Type.Missing )
                    {
                        a.PropertyInfo.SetValue( _stObj, a.Value, null );
                    }
                    else if( !a.IsOptional )
                    {
                        logger.Error( "{0}: Unable to resolve non optional.", a.ToString() );
                    }
                }
            }
            finally
            {
                _ambiantPropertiesResolved = 1;
            }
        }

        interface IAmbiantPropertyGetter
        {
            object GetValue();
        }

        class AmbiantPropertyGetterFromRealProperty : IAmbiantPropertyGetter
        {
            MutableItem _holder;
            PropertyInfo _realProperty;

            public AmbiantPropertyGetterFromRealProperty( MutableItem holder, PropertyInfo realProperty )
            {
                _holder = holder;
                _realProperty = realProperty;
            }

            public object GetValue()
            {
                return _realProperty.GetValue( _holder.StructuredObject, null );
            }
        }

        IAmbiantPropertyGetter LocateAmbiantProperty( IActivityLogger logger, StObjCollectorResult result, IStObjDependencyResolver dependencyResolver, Type propertyType, string name )
        {
            IAmbiantPropertyGetter getter = null;
            MutableItem start = this;
            while( start != null && getter == null )
            {
                if( start._dContainer != null )
                {
                    start._dContainer.EnsureAmbiantPropertiesResolved( logger, result, dependencyResolver );
                    getter = start._dContainer.FindAmbiantProperty( logger, propertyType, name );
                }
                start = start._directGeneralization;
            }
            return getter;
        }

        IAmbiantPropertyGetter FindAmbiantProperty( IActivityLogger logger, Type propertyType, string name )
        {
            var exist = _allAmbiantProperties.FirstOrDefault( a => a.Name == name );
            if( exist == null ) return null;
            // A property exists at the Specialization level, but does it exist for this slice?
            if( !exist.IsDefinedFor( this ) ) return null;
            // Type compatible ?
            if( !propertyType.IsAssignableFrom( exist.Type ) )
            {
                Debug.Assert( exist.Owner == this );
                logger.Warn( "Looking for property named '{0}' of type '{1}': found a candidate on '{2}' but type does not match (it is '{3}'). It is ignored.", name, propertyType.Name, ToString(), exist.Type.Name );
            }
            return new AmbiantPropertyGetterFromRealProperty( exist.Owner, exist.PropertyInfo );
        }

        #region IDependentItemContainer Members

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return null; }
        }

        string IDependentItem.FullName
        {
            get { return _dFullName; }
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

        public object StructuredObject
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

        IStObj IStObj.DirectGeneralization
        {
            get { return _directGeneralization; }
        }

        IStObj IStObj.DirectSpecialization
        {
            get { return _directSpecialization; }
        }

        IStObj IStObj.Generalization
        {
            get { return _generalization; }
        }

        IStObj IStObj.Specialization
        {
            get { return _specialization; }
        }

        IStObj IStObj.Container 
        { 
            get { return _dContainer; } 
        }

        IReadOnlyList<IStObj> IStObj.Requires 
        {
            get { return _dRequires; } 
        }

        IReadOnlyList<IStObj> IStObj.RequiredBy
        {
            get { return _dRequiredBy ?? ReadOnlyListEmpty<IStObj>.Empty; }
        }

        #endregion

    }
}
