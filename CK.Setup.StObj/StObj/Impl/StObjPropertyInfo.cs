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
        readonly PropertyInfo _info;

        public StObjPropertyInfo( string name, Type type, PropertyInfo pInfo )
        {
            Debug.Assert( name != null && type != null );
            Name = name;
            Type = type;
            _info = pInfo;
        }

        internal bool SetValue( IActivityLogger logger, object stObj, object v )
        {
            try
            {
                _info.SetValue( stObj, v, null );
                return true;
            }
            catch( Exception ex )
            {
                logger.Error( ex, "While setting StObj property value on '{0}.{1}'.", _info.DeclaringType.Name, _info.Name );
                return false;
            }
        }
    }
}
