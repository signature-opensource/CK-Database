#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjects\PackageForABLevel1.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Reflection;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    public class PackageForABLevel1 : PackageForAB
    {
        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 1 ), "PackageForAB.Construct has been called." );

            SimpleObjectsTrace.LogMethod( MethodBase.GetCurrentMethod() );

            ConstructCount = ConstructCount + 1;
        }
        
    }
}
