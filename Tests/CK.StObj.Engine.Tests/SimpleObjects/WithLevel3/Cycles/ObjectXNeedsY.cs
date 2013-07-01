using CK.Core;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects.WithLevel3.Cycles
{

    public class ObjectXNeedsY : IAmbientContract
    {
        void Construct( ObjectYNeedsX other )
        {
            Assert.Fail( "Cycle: no object graph initialization." );
        }
    }
}
