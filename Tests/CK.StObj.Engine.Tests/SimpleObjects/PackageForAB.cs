#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjects\PackageForAB.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Reflection;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    [StObj( ItemKind = DependentItemKindSpec.Container )]
    public class PackageForAB : IAmbientContract
    {
        public int ConstructCount { get; protected set; }

        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 0 ), "First construct." );
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            ConstructCount = ConstructCount + 1;
        }
        
    }
}
