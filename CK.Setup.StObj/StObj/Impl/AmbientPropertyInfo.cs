using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace CK.Setup
{
    internal class AmbientPropertyInfo
    {
        readonly PropertyInfo _p;
        readonly bool _isWriteable;
        readonly bool _isMergeable;

        internal AmbientPropertyInfo( StObjTypeInfo owner, PropertyInfo p, AmbientPropertyAttribute attr, bool isWriteable, bool isMergeable )
        {
            Debug.Assert( owner != null );
            _p = p;
            _isWriteable = isWriteable;
            _isMergeable = isMergeable;
            IsOptional = attr.IsOptional;
            ContextAttribute c = (ContextAttribute)Attribute.GetCustomAttribute( p, typeof( ContextAttribute ), false );
            if( c != null ) Context = c.Context;
            else Context = owner.FindContextFromMapAttributes( _p.PropertyType );
        }

        public string Name { get { return _p.Name; } }
        public Type PropertyType { get { return _p.PropertyType; } }
        public Type DeclaringType { get { return _p.DeclaringType; } }
        public PropertyInfo PropertyInfo { get { return _p; } }
        
        /// <summary>
        /// This is settable in order for base class property's context to be used if it is not explicitely defined
        /// by the specialized property.
        /// </summary>
        public Type Context { get; internal set; }
        public bool IsOptional { get; private set; }
        public bool IsWriteable { get { return _isWriteable; } }
        public bool IsMergeable { get { return _isMergeable; } }
    }

}
