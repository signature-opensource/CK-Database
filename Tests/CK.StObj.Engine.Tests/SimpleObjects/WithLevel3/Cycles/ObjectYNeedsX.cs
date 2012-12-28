using CK.Core;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects.WithLevel3.Cycles
{

    public class ObjectYNeedsX : IAmbientContract
    {
        void Construct( ObjectXNeedsY other )
        {
            Assert.Fail( "Cycle: no object graph initialization." );
        }

    }
}
