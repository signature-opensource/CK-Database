using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using CK.Core;

namespace CK.Setup
{
    internal class StObjPropertyInfo
    {
        public readonly string Name;
        public readonly Type Type;
        public readonly PropertyInfo PropertyInfo;

        public StObjPropertyInfo( string name, Type type, PropertyInfo pInfo )
        {
            Debug.Assert( name != null && type != null );
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
    }
}
