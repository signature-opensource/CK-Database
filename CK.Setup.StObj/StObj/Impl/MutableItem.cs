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
        readonly Type _objectType;
        readonly Type _context;
        readonly object _stObj;
        readonly MutableItem _generalization;
        MutableItem _specialization;

        MutableReferenceType _container;
        List<MutableReferenceType> _requires;
        IReadOnlyList<MutableReferenceType> _requiresEx;
        List<MutableReferenceType> _requiredBy;
        IReadOnlyList<MutableReferenceType> _requiredByEx;
        MethodInfo _construct;
        List<MutableParameterType> _constructParameter;
        IReadOnlyList<IMutableParameterType> _constructParameterEx;
        bool _hasBeenReferencedAsAContainer;

        string _dFullName;
        MutableItem _dContainer;
        IReadOnlyList<MutableItem> _dRequires;
        IReadOnlyList<MutableItem> _dRequiredBy;

        internal MutableItem( MutableItem parent, Type context, Type objectType, object theObject )
        {
            Debug.Assert( context != null  && objectType != null && theObject != null );
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
            return AmbiantContractCollector.DisplayName( _context, _objectType );
        }

        public IMutableReferenceType Container 
        {
            get { return _container; }
        }

        public IReadOnlyList<IMutableReferenceType> Requires { get { return _requiresEx; } }

        public IReadOnlyList<IMutableReferenceType> RequiredBy { get { return _requiredByEx; } }

        public IReadOnlyList<IMutableParameterType> ConstructParameters 
        {
            get { return _constructParameterEx; } 
        }

        internal void ApplyAttributes( IActivityLogger logger )
        {
            Func<Type,Type> contextFinder = ReadContextMapperFromStObjContextMapAttribute( logger );
            ApplyStObjAttribute( logger, contextFinder );
            AnalyseConstruct( logger, contextFinder );
        }

        Func<Type, Type> ReadContextMapperFromStObjContextMapAttribute( IActivityLogger logger )
        {
            return t => null;
        }

        void ApplyStObjAttribute( IActivityLogger logger, Func<Type, Type> contextFinder )
        {
            Debug.Assert( _container == null, "Called only once right after object instanciation." );
            _container = new MutableReferenceType( this, MutableReferenceKind.Container );
            var a = StObjAttribute.GetStObjAttribute( _objectType, logger );
            if( a != null )
            {
                _container.Type = a.Container;
                _container.Context = contextFinder( a.Container );
                if( a.Requires != null )
                {
                    _requires = a.Requires.Select( t => new MutableReferenceType( this, MutableReferenceKind.Requires ) { Type = t, Context = contextFinder( t ) } ).ToList();
                }
                else _requires = new List<MutableReferenceType>();
                if( a.RequiredBy != null )
                {
                    _requiredBy = a.RequiredBy.Select( t => new MutableReferenceType( this, MutableReferenceKind.RequiredBy ) { Type = t, Context = contextFinder( t ) } ).ToList();
                }
                else _requiredBy = new List<MutableReferenceType>();
            }
            else
            {
                _requires = new List<MutableReferenceType>();
                _requiredBy = new List<MutableReferenceType>();
            }
            _requiresEx = new ReadOnlyListOnIList<MutableReferenceType>( _requires );
            _requiredByEx = new ReadOnlyListOnIList<MutableReferenceType>( _requiredBy );
        }

        void AnalyseConstruct( IActivityLogger logger, Func<Type, Type> contextFinder )
        {
            Debug.Assert( _constructParameterEx == null, "Called only once right after object instanciation..." );
            Debug.Assert( _container != null, "...and after ApplyStObjAttribute." );

            ParameterInfo[] parameters;
            _construct = _objectType.GetMethod( "Construct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            if( _construct != null && (parameters = _construct.GetParameters()).Length > 0 )
            {
                _constructParameter = new List<MutableParameterType>();
                _constructParameterEx = new ReadOnlyListOnIList<MutableParameterType>( _constructParameter );
                bool hasContainerParameter = false;
                foreach( ParameterInfo i in parameters )
                {
                    // Is it marked with ContainerAttribute?
                    bool isContainerParameter = Attribute.GetCustomAttribute( i, typeof( ContainerAttribute ) ) != null;
                    // If a ContextAttribute exists on the parameter, it takes precedence over the StObjContextMapAttribute on the class.
                    ContextAttribute ctx = (ContextAttribute)Attribute.GetCustomAttribute( i, typeof( ContextAttribute ) );
                    MutableParameterType p;
                    if( ctx != null )
                    {
                        p = new MutableParameterType( this, i, isContainerParameter ) { Context = ctx.Context };
                    }
                    else
                    {
                        p = new MutableParameterType( this, i, isContainerParameter ) { Context = contextFinder( i.ParameterType ) };
                    }
                    if( isContainerParameter )
                    {
                        if( hasContainerParameter )
                        {
                            logger.Error( "Construct method of class '{0}' has more than one parameter marked with [Container] attribute.", _objectType.FullName );
                        }
                        hasContainerParameter = true;
                        if( _container.Type != null )
                        {
                            if( _container.Type != p.Type )
                            {
                                logger.Error( "Construct parameter '{0}' for class '{1}' defines the Container as '{2}' but an attribute on the class declares the Container as '{3}'.", i.Name, _objectType.FullName, p.Type.FullName, _container.Type.FullName );
                            }
                            else if( _container.Context != p.Context )
                            {
                                logger.Error( "Construct parameter '{0}' for class '{1}' targets the Container in '{2}' but an attribute on the class declares the Container context as '{3}'.", i.Name, _objectType.FullName, p.Context.Name, _container.Context.Name );
                            }
                        }
                        // Sets the _container to be the parameter object.
                        _container = p;
                    }
                    _constructParameter.Add( p );
                }
            }
            else
            {
                _constructParameterEx = ReadOnlyListEmpty<MutableParameterType>.Empty;
            }
        }

        void ComputeFullNameAndResolveContainer( IActivityLogger logger, StObjCollectorResult result, StObjCollectorContextualResult contextResult, ref List<MutableItem> prevContainers )
        {
            if( _dFullName != null ) return;

            _dFullName = AmbiantContractCollector.DisplayName( _context, _objectType );
            _dContainer = _container.Resolve( logger, result, contextResult );
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

        internal void PrepareDependentItem( IActivityLogger logger, StObjCollectorResult result, StObjCollectorContextualResult contextResult )
        {
            Debug.Assert( _container != null && _constructParameterEx != null );
            Debug.Assert( _context == contextResult.Context && result[_context] == contextResult, "We are called inside our typed context, this avoids the lookup result[Context] to obtain the owner's context (the default)." );

            // Since we want to remove all the containers of the object from its parameter requirements (see below), 
            // we can not rely on the DependencySorter to detect a cyclic chain of containers:
            // we use the list to collect the chain of containers and detect cycles.
            List<MutableItem> allContainers = null;
            ComputeFullNameAndResolveContainer( logger, result, contextResult, ref allContainers );

            HashSet<MutableItem> req = new HashSet<MutableItem>();
            if( _generalization != null ) req.Add( _generalization );
            foreach( MutableItem dep in _requires.Select( r => r.Resolve( logger, result, contextResult ) ).Where( m => m != null ) )
            {
                req.Add( dep );
            }
            if( _constructParameter != null )
            {
                // We are here considering here that a Container parameter does NOT define a dependency to the whole container (with its content):
                //
                //      That seems strange: we may expect the container to be fully initialized when used as a parameter by a dependency Construct...
                //      The fact is that we are dealing with Objects that have a method Construct, that this Construct method is called on the head
                //      of the container (before any of its content) and that this method has no "thickness", no content in terms of dependencies: its
                //      execution fully initializes the StOj and we can use it.
                //      This is actually fully coherent with the way the setup works. An item of a package does not "require" its own package, it is 
                //      contained in its package and requires some items in the package if needed.
                // 
                foreach( MutableParameterType t in _constructParameter )
                {
                    // Do not consider the container as a requirement since a Container is
                    // already a dependency (on the head's Container) and that a requirement on a container
                    // targets the whole content of it (this would lead to a cycle in the dependency graph).
                    MutableItem dep = t.Resolve( logger, result, contextResult );
                    if( dep != null && (allContainers == null || allContainers.Contains( dep ) == false) ) req.Add( dep );
                }
            }
            _dRequires = req.ToReadOnlyList();

            if( _requiredBy.Count > 0 )
            {
                _dRequiredBy = _requiredBy.Select( r => r.Resolve( logger, result, contextResult ) ).Where( m => m != null ).ToReadOnlyList();
            }
        }

        internal void CallConstruct( IActivityLogger logger, IStObjDependencyResolver dependencyResolver )
        {
            Debug.Assert( _constructParameterEx != null, "Always allocated." );
            
            if( _construct == null ) return;

            object[] parameters = new object[_constructParameterEx.Count];
            int i = 0;
            foreach( MutableParameterType t in _constructParameterEx )
            {
                object instance = Type.Missing;
                // We inject our "setup logger" only if it is exactly the formal parameter: ... , IActivityLogger logger, ...
                // This enforces code homogeneity and let any other IActivityLogger injection.
                if( t.IsSetupLogger )
                {
                    instance = logger;
                }
                else
                {
                    MutableItem resolved = t.Resolved;
                    if( resolved != null )
                    {
                        instance = resolved.StObj;
                    }
                    else if( t.StObjRequirementBehavior != StObjRequirementBehavior.ErrorIfNotStObj )
                    {
                        // Resolve failed to find a StObj, but it was not required to be a StObj.
                        // We try an external resolution with the full data of the parameter (we may call this
                        // with a null Type for instance to enable name based resolution for instance).
                        //
                        // If it fails, we may accept the null depending on IsOptional flag.
                        //
                        if( dependencyResolver != null ) instance = dependencyResolver.Resolve( logger, t );
                    }
                    // By throwing an exception here, we stop the process and avoid the construction 
                    // of an invalid object graph.
                    if( instance == Type.Missing && !t.IsRealParameterOptional )
                    {
                        throw new CKException( "{0}: Unable to resolve non optional.", t.ToString() );
                    }
                    if( instance == null && !t.IsOptional )
                    {
                        throw new CKException( "{0}: Non optional parameter can not be set to null.", t.ToString() );
                    }
                }
                parameters[i++] = instance;
            }
            _construct.Invoke( _stObj, parameters );
        }

        #region IDependentItemContainer Members

        IEnumerable<IDependentItemRef> IDependentItemContainer.Children
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

        public object StObj
        {
            get { return _stObj; }
        }

        public Type ObjectType
        {
            get { return _objectType; }
        }

        public Type Context
        {
            get { return _context; }
        }

        public bool IsContainer
        {
            get { return _hasBeenReferencedAsAContainer; }
        }

        public IStObj Generalization
        {
            get { return _generalization; }
        }

        public IStObj Specialization
        {
            get { return _specialization; }
        }

        public IEnumerable<IStObj> SpecializationPath
        {
            get
            {
                MutableItem s = this;
                do
                {
                    yield return s;
                }
                while( (s = s._specialization) != null );
            }
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
