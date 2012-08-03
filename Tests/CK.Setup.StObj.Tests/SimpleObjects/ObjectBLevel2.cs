using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    public class ObjectBLevel1 : ObjectB
    {
        public ObjectBLevel1()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }
        
        void Contruct()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }
    }
}
