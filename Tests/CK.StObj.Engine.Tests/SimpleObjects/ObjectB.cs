#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjects\ObjectB.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Reflection;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    public class ObjectB : IAmbientContract
    {
        IAbstractionA _a;

        public int ConstructCount { get; protected set; }

        void Construct( [Container]PackageForAB package, IAbstractionA a )
        {
            Assert.That( ConstructCount, Is.EqualTo( 0 ), "First construct." );
            Assert.That( a.ConstructCount, Is.GreaterThanOrEqualTo( 1 ), "At least ObjectA.Construct have been called." );
            Assert.That( package.ConstructCount, Is.GreaterThanOrEqualTo( 1 ), "At least PackageForAB.Construct has been called." );
            
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            _a = a;

            ConstructCount = ConstructCount + 1;
        }
    }
}
