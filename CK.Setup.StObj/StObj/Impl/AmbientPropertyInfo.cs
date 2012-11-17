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
        readonly bool _isValueMergeable;

        internal AmbientPropertyInfo( PropertyInfo p, AmbientPropertyAttribute attr, bool isWriteable, bool isValueMergeable )
        {
            _p = p;
            _isWriteable = isWriteable;
            _isValueMergeable = isValueMergeable;
            IsOptional = attr.IsOptional;
            ContextAttribute c = (ContextAttribute)Attribute.GetCustomAttribute( p, typeof( ContextAttribute ), false );
            if( c != null ) Context = c.Context;
        }

        public string Name { get { return _p.Name; } }
        public Type PropertyType { get { return _p.PropertyType; } }
        public Type DeclaringType { get { return _p.DeclaringType; } }
        public PropertyInfo PropertyInfo { get { return _p; } }
        
        /// <summary>
        /// This is settable in order for base class property's context to be used if it is not explicitely defined
        /// by the specialized property.
        /// </summary>
        public string Context { get; internal set; }
        public bool IsOptional { get; private set; }
        public bool IsWriteable { get { return _isWriteable; } }
        public bool IsValueMergeable { get { return _isValueMergeable; } }
    }

}
