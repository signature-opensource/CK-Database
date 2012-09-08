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

        public bool IsDefinedFor( IStObj stObj )
        {
            if( stObj == null ) throw new ArgumentNullException( "stObj" );
            return _info.DeclaringType.IsAssignableFrom( stObj.ObjectType );
        }

        public override bool SetStructuralValue( IActivityLogger logger, string sourceName, object value )
        {
            if( base.SetStructuralValue( logger, sourceName, value ) )
            {
                try
                {
                    PropertyInfo.SetValue( Owner.StructuredObject, value, null );
                    return true;
                }
                catch( Exception ex )
                {
                    logger.Error( ex, "While setting structural property '{1}.{0}'.", Name, Owner.ObjectType.FullName );
                }
            }
            return false;
        }

    }
}
