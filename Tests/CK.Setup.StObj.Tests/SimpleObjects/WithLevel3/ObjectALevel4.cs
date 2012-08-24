using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects.WithLevel3
{
    public class ObjectALevel4 : ObjectALevel3
    {
        void Construct( IAbstractionBOnLevel2 oB )
        {
            Assert.That( ConstructCount, Is.EqualTo( 4 ), "ObjectA, ObjectALevel1ObjectALevel2 and ObjectALevel3 construct have been called." );
            Assert.That( oB.ConstructCount, Is.GreaterThanOrEqualTo( 3 ), "ObjectB, ObjectBLevel1 and ObjectBLevel2 construct have been called." );

            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            oB.MethofOfBOnLevel2();

            ConstructCount = ConstructCount + 1;
        }

        public override void MethofOfALevel3()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }

    }
}
