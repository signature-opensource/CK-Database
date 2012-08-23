using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    [StObj( Container = typeof( PackageForABLevel1 ) )]
    public class ObjectBLevel1 : ObjectB
    {
        public ObjectBLevel1()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }
        
    }
}
