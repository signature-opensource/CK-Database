using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CK.Setup;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects
{

    [StObj( Container=typeof(PackageForAB) )] 
    public class ObjectA : IAbstractionA
    {
        public int ConstructCount { get; protected set; }

        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 0 ), "First Construct." );
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            ConstructCount = ConstructCount + 1;
        }

        public void MethofOfA()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }

    }
}
