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
using FluentAssertions;

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

        public abstract class A : ABase, IAmbientObject
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
        public void abstract_auto_impl_is_supported_on_non_IAmbientContract_base_class()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
            collector.RegisterType( typeof( A2 ) );
            StObjCollectorResult result = collector.GetResult();
            Assert.That( result.HasFatalError, Is.False );
            Assert.That( result.StObjs.Obtain<A>(), Is.Not.Null.And.AssignableTo<A2>() );
        }

        public abstract class A3 : A
        {
            public abstract A ThirdMethod( int i, string s );
        }

        [Test]
        public void abstract_non_auto_implementable_leaf_are_silently_ignored()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
            collector.RegisterType( typeof( A3 ) );
            StObjCollectorResult result = collector.GetResult();
            result.HasFatalError.Should().BeFalse();
        }

    }
}
