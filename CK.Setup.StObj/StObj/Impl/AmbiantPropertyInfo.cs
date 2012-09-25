using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace CK.Setup
{
    internal class AmbiantPropertyInfo
    {
        readonly PropertyInfo _p;
        
        internal AmbiantPropertyInfo( StObjTypeInfo owner, PropertyInfo p, AmbiantPropertyAttribute attr )
        {
            Debug.Assert( owner != null );
            _p = p;
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

    }

}
