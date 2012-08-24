using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    public class ObjectALevel1 : ObjectA
    {
        ObjectBLevel1 _oB;

        void Construct( [Container]PackageForABLevel1 package, ObjectBLevel1 oB )
        {
            Assert.That( ConstructCount, Is.EqualTo( 1 ), "ObjectA.Construct has been called." );
            Assert.That( oB.ConstructCount, Is.GreaterThanOrEqualTo( 2 ), "ObjectB and ObjectBLevel1 Construct have been called." );
            Assert.That( package.ConstructCount, Is.GreaterThanOrEqualTo( 2 ), "PackageForAB and PackageForABLevel1 Construct have been called." );

            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            _oB = oB;

            ConstructCount = ConstructCount + 1;
        }

    }
}
