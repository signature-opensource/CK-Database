using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Setup
{
    class TrackedAmbientPropertyInfo : ITrackedAmbientPropertyInfo
    {
        public readonly MutableItem SpecializedOwner;
        PropertyInfo PropertyInfo;

        internal TrackedAmbientPropertyInfo( MutableItem o, PropertyInfo p )
        {
            SpecializedOwner = o;
            PropertyInfo = p;
        }

        IStObj ITrackedAmbientPropertyInfo.SpecializedOwner
        {
            get { return SpecializedOwner; }
        }

        PropertyInfo ITrackedAmbientPropertyInfo.PropertyInfo
        {
            get { return PropertyInfo; }
        }
    }
}
