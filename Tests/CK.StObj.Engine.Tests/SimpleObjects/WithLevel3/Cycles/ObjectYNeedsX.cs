#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjects\WithLevel3\Cycles\ObjectYNeedsX.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects.WithLevel3.Cycles
{

    public class ObjectYNeedsX : IAmbientContract
    {
        void StObjConstruct( ObjectXNeedsY other )
        {
            // See comments in ObjectXNeedsY constructor.
            Assert.Fail( "Cycle: no object graph initialization." );
        }

    }
}
