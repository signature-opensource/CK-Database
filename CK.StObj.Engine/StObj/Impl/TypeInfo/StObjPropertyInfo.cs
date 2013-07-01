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
        public readonly PropertyResolutionSource ResolutionSource;

        public StObjPropertyInfo( Type declaringType, PropertyResolutionSource source, string name, Type type, PropertyInfo pInfo )
        {
            Debug.Assert( declaringType != null && name != null && type != null );
            DeclaringType = declaringType;
            Name = name;
            Type = type;
            PropertyInfo = pInfo;
            ResolutionSource = source;
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
