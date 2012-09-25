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
    /// Describes an Ambiant property.
    /// </summary>
    internal class MutableAmbiantProperty : MutableResolvableReference, IMutableAmbiantProperty, IAmbiantProperty
    {
        AmbiantPropertyInfo _info;

        internal MutableAmbiantProperty( MutableItem owner, AmbiantPropertyInfo info )
            : base( owner, MutableReferenceKind.AmbiantProperty )
        {
            _info = info;
            Type = _info.PropertyType;
            IsOptional = _info.IsOptional;
        }

        public override string Name { get { return _info.Name; } }

        internal override string KindName { get { return "AmbiantProperty"; } }

        internal override Type UnderlyingType { get { return _info.PropertyType; } }

        public override string ToString()
        {
            string s = String.Format( "Ambiant Property '{0}' of '{1}'", Name, Owner.ToString() );
            return s;
        }

        internal PropertyInfo PropertyInfo { get { return _info.PropertyInfo; } }

        bool IMutableAmbiantProperty.IsDefinedFor( IStObjMutableItem stObj )
        {
            if( stObj == null ) throw new ArgumentNullException( "stObj" );
            return IsDefinedFor( stObj.ObjectType );
        }

        bool IAmbiantProperty.IsDefinedFor( IStObj stObj )
        {
            if( stObj == null ) throw new ArgumentNullException( "stObj" );
            return IsDefinedFor( stObj.ObjectType );
        }

        internal bool IsDefinedFor( Type t )
        {
            Debug.Assert( t != null );
            return _info.DeclaringType.IsAssignableFrom( t );
        }

    }
}
