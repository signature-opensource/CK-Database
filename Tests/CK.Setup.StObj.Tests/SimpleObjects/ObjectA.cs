using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CK.Setup;

namespace CK.Setup.StObj.Tests.SimpleObjects
{

    [StObj( Container=typeof(PackageForAB) )] 
    public class ObjectA : IAbstractionA
    {
        public ObjectA()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }

        void Contruct()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }

        public void MethofOfA()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }

    }
}
