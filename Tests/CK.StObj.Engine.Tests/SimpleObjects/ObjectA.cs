#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjects\ObjectA.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Reflection;
using NUnit.Framework;
using CK.Setup;

namespace CK.StObj.Engine.Tests.SimpleObjects
{

    [StObj( Container=typeof(PackageForAB) )] 
    public class ObjectA : IAbstractionA
    {
        public int ConstructCount { get; protected set; }

        void Construct()
        { 
            Assert.That( ConstructCount, Is.EqualTo( 0 ), "First Construct." );
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            ConstructCount = ConstructCount + 1;
        }

        public void MethofOfA()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }

    }
}
