using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    [StObj( Container = typeof( PackageForABLevel1 ) )]
    public class ObjectALevel1 : ObjectA
    {
        ObjectBLevel1 _oB;

        public ObjectALevel1()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }

        void Contruct( ObjectBLevel1 oB )
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            _oB = oB;
        }

    }
}
