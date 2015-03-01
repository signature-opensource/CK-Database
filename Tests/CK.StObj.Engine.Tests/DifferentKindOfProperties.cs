#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\DifferentKindOfProperties.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class DifferentKindOfProperties
    {

        class ObjA : IAmbientContract
        {
            [AmbientProperty]
            public ObjB NoProblem { get; set; }
        }

        class ObjB : IAmbientContract
        {
            [StObjProperty]
            [AmbientProperty]
            public ObjA TwoAttributes { get; set; } 
        }

        class ObjSpecA : ObjA
        {
            [StObjProperty]
            public new ObjB NoProblem { get; set; }
        }

        [StObjProperty( PropertyName = "NoProblem", PropertyType = typeof(object) )]
        class ObjSpecA2 : ObjA
        {
        }

        [Test]
        public void StObjAndAmbientPropertiesAreIncompatible()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( ObjB ) );
                collector.RegisterClass( typeof( ObjA ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( ObjA ) );
                collector.RegisterClass( typeof( ObjSpecA ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( ObjA ) );
                collector.RegisterClass( typeof( ObjSpecA2 ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
        }

        // A null property type triggers an error: it must be explicitely typeof(object).
        [StObjProperty( PropertyName = "AProperty", PropertyType = null )]
        class MissingStObjPropertyType : IAmbientContract
        {
        }

        [StObjProperty( PropertyName = "  " )]
        class MissingStObjPropertyName : IAmbientContract
        {
        }

        [StObjProperty( PropertyName = "Albert", PropertyType = typeof(object) )]
        class DuplicateStObjProperty : IAmbientContract
        {
            [StObjProperty]
            public object Albert { get; set; }
        }

        [Test]
        public void InvalidStObjProperties()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( MissingStObjPropertyType ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( MissingStObjPropertyName ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( DuplicateStObjProperty ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
        }


        class InvalidAmbientContractProperty : IAmbientContract
        {
            [InjectContract]
            public DifferentKindOfProperties NotAnIAmbientContractProperty { get; protected set; }
        }

        [Test]
        public void AmbientContractsMustBeAmbientContracts()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( InvalidAmbientContractProperty ) );
                var r = collector.GetResult();
                Assert.That( r.HasFatalError );
            }
        }

        #region Covariance support

        class CA : IAmbientContract
        {
        }

        class CA2 : CA
        {
        }

        class CA3 : CA2
        {
        }

        class CB : IAmbientContract
        {
            [InjectContract]
            public CA A { get; set; }
        }

        class CB2 : CB
        {
            [InjectContract]
            public new CA2 A { get { return (CA2)base.A; } }
        }

        class CB3 : CB2
        {
            [InjectContract]
            public new CA3 A 
            {
                get { return (CA3)base.A; }
                set
                {
                    Assert.Fail( "This is useless and is not called. This setter generates a warning." );
                }
            }
        }

        [Test]
        public void CovariantPropertiesSupport()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( CB3 ) );
                collector.RegisterClass( typeof( CA3 ) );
                var r = collector.GetResult();
                Assert.That( r.HasFatalError, Is.False );
                var cb = r.Default.StObjMap.Obtain<CB>();
                Assert.That( cb, Is.InstanceOf<CB3>() );
                Assert.That( cb.A, Is.InstanceOf<CA3>() );
            }
        }

        class CMissingSetterOnTopDefiner : IAmbientContract
        {
            [InjectContract]
            public CA2 A { get { return null; } }
        }

        [Test]
        public void SetterMustExistOnTopDefiner()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( CMissingSetterOnTopDefiner ) );
                collector.RegisterClass( typeof( CA2 ) );
                Assert.That( collector.RegisteringFatalOrErrorCount, Is.EqualTo( 1 ) );
            }
        }

        class CPrivateSetter : IAmbientContract
        {
            [InjectContract]
            public CA2 A { get; private set; }
        }

        [Test]
        public void PrivateSetterWorks()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
                collector.RegisterClass( typeof( CPrivateSetter ) );
                collector.RegisterClass( typeof( CA2 ) );
                var r = collector.GetResult();
                Assert.That( r.HasFatalError, Is.False );
                var c = r.Default.StObjMap.Obtain<CPrivateSetter>();
                Assert.That( c.A, Is.InstanceOf<CA2>() );
            }
        }


        #endregion

    }
}
