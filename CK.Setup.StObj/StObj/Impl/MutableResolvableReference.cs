using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;

namespace CK.Setup
{
    /// <summary>
    /// Base class for construct parameters or ambiant properties: these references can be resolved
    /// either structurally or dynamically (by <see cref="IStObjDependencyResolver"/>).
    /// </summary>
    internal abstract class MutableResolvableReference : MutableReference, IResolvableReference
    {
        MutableItem _resolved;

        internal MutableResolvableReference( MutableItem owner, MutableReferenceKind kind )
            : base( owner, kind )
        {
            _resolved = UnresolvedMarker;
            Value = Type.Missing;
        }

        public abstract string Name { get; }

        internal abstract string KindName { get; }

        internal abstract Type UnderlyingType { get; }

        public bool IsOptional { get; set; }

        public object Value { get; set; }

        internal bool HasBeenResolved { get { return Value != Type.Missing; } }

        internal MutableItem CachedResolvedStObj 
        { 
            get 
            {
                Debug.Assert( _resolved != UnresolvedMarker, "ResolveToStObj must have been called before." );
                return _resolved; 
            } 
        }

        internal override MutableItem ResolveToStObj( IActivityLogger logger, StObjCollectorResult collector, StObjCollectorContextualResult ownerCollector )
        {
            if( _resolved != UnresolvedMarker ) return _resolved;
            if( Type == null && !IsOptional )
            {
                Error( logger, String.Format( "Type can not be null since the {0} is not optional", KindName ) );
                return _resolved = null;
            }
            Debug.Assert( Type != null || IsOptional );
            if( Type != null )
            {
                if( !UnderlyingType.IsAssignableFrom( Type ) )
                {
                    Error( logger, String.Format( "Type '{0}' is not compatible with the {1} type ('{2}')", Type.FullName, KindName, UnderlyingType.FullName ) );
                    return _resolved = null;
                }
            }
            return _resolved = base.ResolveToStObj( logger, collector, ownerCollector );
        }

        public virtual bool SetResolvedValue( IActivityLogger logger, object value )
        {
            Value = value;
            return true;
        }

        public virtual bool SetStructuralValue( IActivityLogger logger, string sourceName, object value )
        {
            if( sourceName == null ) throw new ArgumentNullException( "sourceName" );
            if( value == Type.Missing ) throw new InvalidOperationException( "Setting a structural value to Type.Missing is not allowed. Source = " + sourceName );
            Value = value;
            return true;
        }

        IStObj IResolvableReference.Owner
        {
            get { return Owner; }
        }
    }
}
