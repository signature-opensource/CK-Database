using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects.WithLevel3.Cycles
{
    public class ObjectBLevel3_InPackageForAB : ObjectBLevel2
    {
        void Construct( [Container]PackageForAB package )
        {
            Assert.Fail( "Since this creates a Cycle, the object graph is not created." );
        }

    }
}
