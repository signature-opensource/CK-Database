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

    public class ObjectXNeedsY : IAmbientContract
    {
        void StObjConstruct( ObjectYNeedsX other )
        {
            Assert.Fail( "Cycle: no object graph initialization." );
        }
    }
}
