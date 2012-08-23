using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    [StObj( Container = typeof( PackageForAB ) )]
    public class ObjectB : IAmbiantContract
    {
        IAbstractionA _a;

        public ObjectB()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }
        
        void Contruct( IAbstractionA a )
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            _a = a;
        }
    }
}
