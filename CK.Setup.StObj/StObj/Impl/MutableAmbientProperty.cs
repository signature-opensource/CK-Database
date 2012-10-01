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
    /// Describes an Ambient property.
    /// </summary>
    internal class MutableAmbientProperty : MutableResolvableReference, IMutableAmbientProperty, IAmbientProperty
    {
        AmbientPropertyInfo _info;

        internal MutableAmbientProperty( MutableItem owner, AmbientPropertyInfo info )
            : base( owner, MutableReferenceKind.AmbientProperty )
        {
            _info = info;
            Type = _info.PropertyType;
            IsOptional = _info.IsOptional;
        }

        public override string Name { get { return _info.Name; } }

        internal override string KindName { get { return "AmbientProperty"; } }

        internal override Type UnderlyingType { get { return _info.PropertyType; } }

        public override string ToString()
        {
            string s = String.Format( "Ambient Property '{0}' of '{1}'", Name, Owner.ToString() );
            return s;
        }

        internal bool IsMergeable { get { return _info.IsMergeable; } }
        
        internal bool IsWriteable { get { return _info.IsWriteable; } }

        internal PropertyInfo PropertyInfo { get { return _info.PropertyInfo; } }

        bool IMutableAmbientProperty.IsDefinedFor( IStObjMutableItem stObj )
        {
            if( stObj == null ) throw new ArgumentNullException( "stObj" );
            return IsDefinedFor( stObj.ObjectType );
        }

        bool IAmbientProperty.IsDefinedFor( IStObj stObj )
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
