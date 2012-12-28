using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Setup
{
    class TrackedAmbientPropertyInfo : IStObjTrackedAmbientPropertyInfo
    {
        public readonly MutableItem Owner;
        public readonly AmbientPropertyInfo AmbientPropertyInfo;

        internal TrackedAmbientPropertyInfo( MutableItem o, AmbientPropertyInfo p )
        {
            Owner = o;
            AmbientPropertyInfo = p;
        }

        IStObj IStObjTrackedAmbientPropertyInfo.Owner
        {
            get { return Owner; }
        }

        PropertyInfo IStObjTrackedAmbientPropertyInfo.PropertyInfo
        {
            get { return AmbientPropertyInfo.PropertyInfo; }
        }
    }
}
