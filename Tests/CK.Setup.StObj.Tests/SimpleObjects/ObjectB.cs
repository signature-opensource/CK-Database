using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    public class ObjectB : IAmbiantContract
    {
        IAbstractionA _a;

        public int ConstructCount { get; protected set; }

        void Construct( [Container]PackageForAB package, IAbstractionA a )
        {
            Assert.That( ConstructCount, Is.EqualTo( 0 ), "First construct." );
            Assert.That( a.ConstructCount, Is.GreaterThanOrEqualTo( 1 ), "At least ObjectA.Construct have been called." );
            Assert.That( package.ConstructCount, Is.GreaterThanOrEqualTo( 1 ), "At least PackageForAB.Construct has been called." );
            
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            _a = a;

            ConstructCount = ConstructCount + 1;
        }
    }
}
