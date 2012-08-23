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
    internal class MutableItem : IStObj, IStObjMutableItem, IDependentItemContainer, IDependentItemContainerRef
    {
        readonly Type _objectType;
        readonly Type _context;
        readonly object _stObj;
        
        MutableReferenceType _container;
        List<MutableReferenceType> _requires;
        IReadOnlyList<MutableReferenceType> _requiresEx;
        List<MutableReferenceType> _requiredBy;
        IReadOnlyList<MutableReferenceType> _requiredByEx;
        MethodInfo _construct;
        List<MutableParameterType> _constructParameter;
        IReadOnlyList<IMutableParameterType> _constructParameterEx;

        string _dFullName;
        MutableItem _dContainer;
        IEnumerable<MutableItem> _dRequires;
        IEnumerable<MutableItem> _dRequiredBy;

        internal MutableItem( Type context, Type objectType, object theObject )
        {
            Debug.Assert( context != null  && objectType != null && theObject != null );
            _objectType = objectType;
            _stObj = theObject;
            _context = context;
        }

        public Type ObjectType
        {
            get { return _objectType; }
        }

        public Type Context
        {
            get { return _context; }
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
                _requires = a.Requires.Select( t => new MutableReferenceType( this, MutableReferenceKind.Requires ) { Type = t, Context = contextFinder( t ) } ).ToList();
                _requiredBy = a.RequiredBy.Select( t => new MutableReferenceType( this, MutableReferenceKind.RequiredBy ) { Type = t, Context = contextFinder( t ) } ).ToList();
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
            Debug.Assert( _constructParameter == null, "Called only once right after object instanciation." );

            ParameterInfo[] parameters;
            _construct = _objectType.GetMethod( "Construct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            if( _construct != null && (parameters = _construct.GetParameters()).Length > 0 )
            {
                _constructParameter = new List<MutableParameterType>();
                _constructParameterEx = new ReadOnlyListOnIList<MutableParameterType>( _constructParameter );
                foreach( ParameterInfo i in parameters )
                {
                    // If a StObjContextAttribute exists on the parameter, it takes precedence over the StObjContextMapAttribute on the class.
                    StObjContextAttribute ctx = (StObjContextAttribute)Attribute.GetCustomAttribute( i, typeof( StObjContextAttribute ) );
                    MutableParameterType p;
                    if( ctx != null )
                    {
                        p = new MutableParameterType( this, _constructParameter.Count, i.Name ) { Type = i.ParameterType, Context = ctx.Context };
                    }
                    else
                    {
                        p = new MutableParameterType( this, _constructParameter.Count, i.Name ) { Type = i.ParameterType, Context = contextFinder( i.ParameterType ) };
                    }
                    _constructParameter.Add( p );
                }
            }
            else
            {
                _constructParameterEx = ReadOnlyListEmpty<MutableParameterType>.Empty;
            }
        }


        internal void PrepareDependentItem( IActivityLogger logger, StObjCollectorResult result, StObjCollectorContextualResult contextResult )
        {
            Debug.Assert( _container != null && _constructParameter != null );
            Debug.Assert( _context == contextResult.Context && result[_context] == contextResult, "We are called inside our typed context, this avoids the lookup result[Context]." );

            _dFullName = AmbiantContractCollector.DisplayName( _context, _objectType );
            _dContainer = _container.Resolve( logger, result, contextResult );
            HashSet<MutableItem> req = null;
            foreach( MutableItem dep in _requires.Select( r => r.Resolve( logger, result, contextResult ) ).Where( m => m != null ) )
            {
                if( req == null ) req = new HashSet<MutableItem>();
                req.Add( dep );
            }
            if( _constructParameter != null )
            {
                foreach( MutableParameterType t in _constructParameter )
                {
                    MutableItem dep = t.Resolve( logger, result, contextResult );
                    if( dep != null )
                    {
                        if( req == null ) req = new HashSet<MutableItem>();
                        req.Add( dep );
                    }
                }
            }
            if( req != null ) _dRequires = req.ToArray();

            if( _requiredBy.Count > 0 )
            {
                _dRequiredBy = _requiredBy.Select( r => r.Resolve( logger, result, contextResult ) ).Where( m => m != null ).ToArray();
            }
        }

        internal void CallConstruct( IActivityLogger logger, StObjCollectorResult result, IStObjDependencyResolver dependencyResolver )
        {
            if( _construct == null ) return;
            Debug.Assert( _constructParameterEx != null, "Always allocated." );           

            object[] parameters = new object[_constructParameterEx.Count];
            int i = 0;
            foreach( MutableParameterType t in _constructParameterEx )
            {
                object instance = null;
                MutableItem resolved = t.Resolved;
                if( resolved != null )
                {
                    instance = resolved.StObj;
                }
                else if( !t.StObjRequired )
                {
                    // Resolve failed to find a StObj, but it was not required to be a StObj.
                    // We try an external resolution. If it fails, we may accept the null
                    // depending on IsOptional flag.
                    if( dependencyResolver != null ) instance = dependencyResolver.Resolve( t );
                }
                // By throwing an exception here, we stop the process and avoid the construction 
                // of an invalid object graph.
                if( instance == null && !t.IsOptional )
                {
                    throw new CKException( "Unable to resolve non optional: {0}", t.ToString() );
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

        #endregion

    }
}
