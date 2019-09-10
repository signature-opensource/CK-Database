#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjects\WithLevel3\Cycles\ObjectXNeedsY.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects.WithLevel3.Cycles
{


    public class ObjectXNeedsY : IRealObject
    {
        void StObjConstruct( ObjectYNeedsX other )
        {
            // This ObjectXNeedsY along with ObjectYNeedsX is used in two scenarii:
            // - They create a cycle: this was tested by the following.
            //   Assert.Fail( "Cycle: no object graph initialization." );
            // - It is also tested without the ObjectYNeedsX missing in registration.
            //   In such case, there is NO cycle, but the missing reference is
            //   detected when the graph is Constructed and a default value (null) is
            //   injected in order for other errors to be detected.
            //   ==> SimpleObjectsTests.MissingReference will success (result.HasFatal is true)
            //       but if we Assert.Fail here, NUnit 3.10.1 consider the test to have failed.
            //       This is why we HARD FAIL only if this construct is called while the ObjectYNeedsX
            //       is available.
            if( other != null ) Assert.Fail( "Cycle: no object graph initialization." );
        }
    }
}
