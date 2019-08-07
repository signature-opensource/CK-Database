using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;

using static CK.Testing.MonitorTestHelper;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class DifferentKindOfProperties
    {

        class ObjA : IAmbientObject
        {
            [AmbientProperty]
            public ObjB NoProblem { get; set; }
        }

        class ObjB : IAmbientObject
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
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( ObjB ) );
                collector.RegisterType( typeof( ObjA ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( ObjA ) );
                collector.RegisterType( typeof( ObjSpecA ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( ObjA ) );
                collector.RegisterType( typeof( ObjSpecA2 ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
        }

        // A null property type triggers an error: it must be explicitly typeof(object).
        [StObjProperty( PropertyName = "AProperty", PropertyType = null )]
        class MissingStObjPropertyType : IAmbientObject
        {
        }

        [StObjProperty( PropertyName = "  " )]
        class MissingStObjPropertyName : IAmbientObject
        {
        }

        [StObjProperty( PropertyName = "Albert", PropertyType = typeof(object) )]
        class DuplicateStObjProperty : IAmbientObject
        {
            [StObjProperty]
            public object Albert { get; set; }
        }

        [Test]
        public void InvalidStObjProperties()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( MissingStObjPropertyType ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( MissingStObjPropertyName ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( DuplicateStObjProperty ) );
                Assert.That( collector.RegisteringFatalOrErrorCount == 1 );
            }
        }

        class ScopedService : IScopedAmbientService { }

        class InvalidAmbientObjectProperty : IAmbientObject
        {
            [InjectObject]
            public ScopedService NotAnAmbientObjectPropertyType { get; protected set; }
        }

        [Test]
        public void InjectSingleton_must_not_be_scoped_service()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( InvalidAmbientObjectProperty ) );
                collector.GetResult().HasFatalError.Should().BeTrue();
            }
        }

        #region Covariance support

        class CA : IAmbientObject
        {
        }

        class CA2 : CA
        {
        }

        class CA3 : CA2
        {
        }

        class CB : IAmbientObject
        {
            [InjectObjectAttribute]
            public CA A { get; set; }
        }

        class CB2 : CB
        {
            [InjectObjectAttribute]
            public new CA2 A { get { return (CA2)base.A; } }
        }

        class CB3 : CB2
        {
            [InjectObjectAttribute]
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
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( CB3 ) );
                collector.RegisterType( typeof( CA3 ) );
                var r = collector.GetResult(  );
                Assert.That( r.HasFatalError, Is.False );
                var cb = r.StObjs.Obtain<CB>();
                Assert.That( cb, Is.InstanceOf<CB3>() );
                Assert.That( cb.A, Is.InstanceOf<CA3>() );
            }
        }

        class CMissingSetterOnTopDefiner : IAmbientObject
        {
            [InjectObjectAttribute]
            public CA2 A { get { return null; } }
        }

        [Test]
        public void SetterMustExistOnTopDefiner()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( CMissingSetterOnTopDefiner ) );
                collector.RegisterType( typeof( CA2 ) );
                Assert.That( collector.RegisteringFatalOrErrorCount, Is.EqualTo( 1 ) );
            }
        }

        class CPrivateSetter : IAmbientObject
        {
            [InjectObjectAttribute]
            public CA2 A { get; private set; }
        }

        [Test]
        public void PrivateSetterWorks()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterType( typeof( CPrivateSetter ) );
                collector.RegisterType( typeof( CA2 ) );
                var r = collector.GetResult( );
                Assert.That( r.HasFatalError, Is.False );
                var c = r.StObjs.Obtain<CPrivateSetter>();
                Assert.That( c.A, Is.InstanceOf<CA2>() );
            }
        }


        #endregion

    }
}
