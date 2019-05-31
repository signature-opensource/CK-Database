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

        void StObjConstruct( SimpleAmbientService service )
        { 
            Assert.That( ConstructCount, Is.EqualTo( 0 ), "First StObjConstruct.");
            SimpleObjectsTrace.LogMethod( GetType().GetMethod( "StObjConstruct", BindingFlags.Instance|BindingFlags.NonPublic ) );
            ConstructCount = ConstructCount + 1;
            Assert.That( service, Is.Null, "We inject null for services." );
        }

        public void MethofOfA()
        {
            SimpleObjectsTrace.LogMethod( GetType().GetMethod( "MethofOfA", BindingFlags.Instance | BindingFlags.Public ) );
        }

    }
}
