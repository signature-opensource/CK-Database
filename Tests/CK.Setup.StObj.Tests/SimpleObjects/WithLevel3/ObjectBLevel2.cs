using System.Reflection;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects.WithLevel3
{
    // Container PackageForABLevel1 is "over defined" here:
    // - by the StObj attribute.
    // - by the Construct parameter.
    // - by its base class (ObjectBLevel1 is already in PackageForABLevel1).
    //
    // As long as it is the same, there is no problem.
    //
    [StObj( Container = typeof( PackageForABLevel1 ) )]
    public class ObjectBLevel2 : ObjectBLevel1, IAbstractionBOnLevel2
    {
        IAbstractionALevel3 _a3;

        void Construct( [Container]PackageForABLevel1 package, IAbstractionALevel3 a3 )
        {
            Assert.That( ConstructCount, Is.EqualTo( 2 ), "ObjectB, ObjectBLevel1 construct have been called." );
            Assert.That( a3.ConstructCount, Is.GreaterThanOrEqualTo( 4 ), "ObjectA, ObjectALevel1, ObjectALevel2 and ObjectALevel3.Construct have been called." );

            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            a3.MethofOfALevel3();
            _a3 = a3;

            ConstructCount = ConstructCount + 1;
        }

        public virtual void MethofOfBOnLevel2()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }
    }
}
