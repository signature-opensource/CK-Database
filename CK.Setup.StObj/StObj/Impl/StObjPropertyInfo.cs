using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using CK.Core;

namespace CK.Setup
{
    internal class StObjPropertyInfo : INamedPropertyInfo
    {
        public readonly string Name;
        public readonly Type Type;
        public readonly PropertyInfo PropertyInfo;

        public StObjPropertyInfo( Type declaringType, string name, Type type, PropertyInfo pInfo )
        {
            Debug.Assert( declaringType != null && name != null && type != null );
            DeclaringType = declaringType;
            Name = name;
            Type = type;
            PropertyInfo = pInfo;
        }

        internal bool SetValue( IActivityLogger logger, object stObj, object v )
        {
            Debug.Assert( PropertyInfo != null );
            try
            {
                PropertyInfo.SetValue( stObj, v, null );
                return true;
            }
            catch( Exception ex )
            {
                logger.Error( ex, "While setting StObj property value on '{0}.{1}'.", PropertyInfo.DeclaringType.Name, PropertyInfo.Name );
                return false;
            }
        }

        public Type DeclaringType { get; private set; }

        string INamedPropertyInfo.Name
        {
            get { return Name; }
        }

        string INamedPropertyInfo.Kind
        {
            get { return "[StObjProperty]"; }
        }

    }
}
