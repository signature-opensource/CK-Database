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
    /// Base class for construct parameters, ambient properties or Ambient contracts.
    /// </summary>
    internal abstract class MutableReferenceOptional : MutableReference
    {
        MutableItem _resolved;

        internal MutableReferenceOptional( MutableItem owner, StObjMutableReferenceKind kind )
            : base( owner, kind )
        {
            _resolved = UnresolvedMarker;
        }

        public abstract string Name { get; }

        internal abstract string KindName { get; }

        internal abstract Type UnderlyingType { get; }

        public bool IsOptional { get; set; }

        internal MutableItem CachedResolvedStObj 
        { 
            get 
            {
                Debug.Assert( _resolved != UnresolvedMarker, "ResolveToStObj must have been called before." );
                return _resolved; 
            } 
        }

        internal override MutableItem ResolveToStObj( IActivityMonitor monitor, StObjCollectorResult collector, StObjCollectorContextualResult cachedCollector )
        {
            if( _resolved != UnresolvedMarker ) return _resolved;
            if( Type == null && !IsOptional )
            {
                Error( monitor, String.Format( "Type can not be null since the {0} is not optional", KindName ) );
                return _resolved = null;
            }
            Debug.Assert( Type != null || IsOptional );
            if( Type != null )
            {
                if( !UnderlyingType.IsAssignableFrom( Type ) )
                {
                    Error( monitor, String.Format( "Type '{0}' is not compatible with the {1} type ('{2}')", Type.FullName, KindName, UnderlyingType.FullName ) );
                    return _resolved = null;
                }
            }
            return _resolved = base.ResolveToStObj( monitor, collector, cachedCollector );
        }
    }
}
