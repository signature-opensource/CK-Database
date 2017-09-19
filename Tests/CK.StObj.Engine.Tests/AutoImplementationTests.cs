#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\AutoImplementationTests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;
using CK.Setup;
using NUnit.Framework;
using System.Diagnostics;

namespace CK.StObj.Engine.Tests
{

    [TestFixture]
    public class AutoImplementationTests
    {

        [AttributeUsage( AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
        public class AutoImplementMethodAttribute : Attribute, IAttributeAutoImplemented
        {
        }

        public abstract class ABase
        {
            [AutoImplementMethod]
            protected abstract int FirstMethod( int i );
        }

        public abstract class A : ABase, IAmbientContract
        {
            [AutoImplementMethod]
            public abstract string SecondMethod( int i );
        }

        public abstract class A2 : A
        {
            [AutoImplementMethod]
            public abstract A ThirdMethod( int i, string s );
        }

        [Test]
        public void AbstractDetection()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Monitor );
            collector.RegisterClass( typeof( A2 ) );
            StObjCollectorResult result = collector.GetResult( new SimpleServiceContainer() );
            Assert.That( result.HasFatalError, Is.False );
            Assert.That( result.Default.StObjMap.Obtain<A>(), Is.Not.Null.And.AssignableTo<A2>() );
        }

    }
}
