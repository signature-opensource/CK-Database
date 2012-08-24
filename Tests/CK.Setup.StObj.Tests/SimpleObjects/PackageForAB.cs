using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    public class PackageForAB : IAmbiantContract
    {
        public int ConstructCount { get; protected set; }

        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 0 ), "First construct." );
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            ConstructCount = ConstructCount + 1;
        }
        
    }
}
