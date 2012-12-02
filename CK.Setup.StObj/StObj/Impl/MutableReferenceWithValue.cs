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
    /// Base class for construct parameters or ambient properties: these references can be resolved
    /// either structurally or dynamically (by <see cref="IStObjValueResolver"/>).
    /// </summary>
    internal abstract class MutableReferenceWithValue : MutableReference
    {
        MutableItem _resolved;

        internal MutableReferenceWithValue( MutableItem owner, StObjMutableReferenceKind kind )
            : base( owner, kind )
        {
            _resolved = UnresolvedMarker;
            Value = Type.Missing;
        }

        public abstract string Name { get; }

        internal abstract string KindName { get; }

        internal abstract Type UnderlyingType { get; }

        public bool IsOptional { get; set; }

        public object Value { get; protected set; }

        internal bool HasBeenSet { get { return Value != Type.Missing; } }

        internal MutableItem CachedResolvedStObj 
        { 
            get 
            {
                Debug.Assert( _resolved != UnresolvedMarker, "ResolveToStObj must have been called before." );
                return _resolved; 
            } 
        }

        internal override MutableItem ResolveToStObj( IActivityLogger logger, StObjCollectorResult collector, StObjCollectorContextualResult cachedCollector )
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
            return _resolved = base.ResolveToStObj( logger, collector, cachedCollector );
        }
    }
}
