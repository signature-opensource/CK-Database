using CK.Core;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects.WithLevel3.Cycles
{

    public class ObjectXNeedsY : IAmbiantContract
    {
        void Construct( ObjectYNeedsX other )
        {
            Assert.Fail( "Cycle: no object graph initialization." );
        }
    }
}
