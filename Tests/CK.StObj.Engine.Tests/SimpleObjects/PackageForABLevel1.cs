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
        void StObjConstruct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 1 ), "PackageForAB.StObjConstruct has been called.");

            SimpleObjectsTrace.LogMethod( GetType().GetMethod( "StObjConstruct", BindingFlags.Instance | BindingFlags.NonPublic ) );

            ConstructCount = ConstructCount + 1;
        }
        
    }
}
