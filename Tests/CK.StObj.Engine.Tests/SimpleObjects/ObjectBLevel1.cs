#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjects\ObjectBLevel1.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Reflection;
using NUnit.Framework;
using CK.Setup;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    [StObj( Container = typeof( PackageForABLevel1 ) )]
    public class ObjectBLevel1 : ObjectB
    {
        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 1 ), "ObjectB.Construct has been called." );
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            ConstructCount = ConstructCount + 1;
        }
    }
}
