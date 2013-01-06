using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.Setup;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class DynamicGenerationTests
    {
        public class CSimpleEmit
        {

            class AutoImplementedAttribute : Attribute, IAutoImplementorMethod
            {
                public bool Implement( IActivityLogger logger, System.Reflection.MethodInfo m, System.Reflection.Emit.TypeBuilder b, bool isVirtual )
                {
                    CK.Reflection.EmitHelper.ImplementEmptyStubMethod( b, m, isVirtual );
                    return true;
                }
            }

            public class A : IAmbientContract
            {
            }

            public abstract class B : A
            {
                [AutoImplemented]
                public abstract int Auto( int i );
            }

            public interface IC : IAmbientContract
            {
                A TheA { get; }
            }

            public class C : IC
            {
                [AmbientContract]
                public A TheA { get; private set; }
            }

            public class D : C
            {
                [AmbientProperty( IsOptional = true )]
                public string AnOptionalString { get; private set; }
            }

            public void DoTest()
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterClass( typeof( B ) );
                collector.RegisterClass( typeof( D ) );
                collector.DependencySorterHookInput = TestHelper.Trace;
                collector.DependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
                var r = collector.GetResult();
                Assert.That( r.HasFatalError, Is.False );
                // Null as directory => use CK.StObj.Model folder.
                r.GenerateFinalAssembly( TestHelper.Logger, null, "TEST_SimpleEmit" );

                StObjContextRoot c = StObjContextRoot.Load( "TEST_SimpleEmit", TestHelper.Logger );
                Assert.That( typeof( B ).IsAssignableFrom( c.Default.ToLeafType( typeof( A ) ) ) );
                Assert.That( c.Default.ToLeafType( typeof( IC ) ), Is.SameAs( typeof( D ) ) );
                Assert.That( c.Default.Obtain<B>().Auto(3), Is.EqualTo( 0 ) );
            }

        }

        [Test]
        public void SimpleEmit()
        {
            new CSimpleEmit().DoTest();
        }

        public class CConstructCalledAndStObjProperties
        {
            public class A : IAmbientContract
            {
                [StObjProperty]
                public string StObjPower { get; set; }

                void Construct( IActivityLogger logger )
                {
                    logger.Trace( "At A level: StObjPower = '{0}'.", StObjPower );
                }
            }

            public class ASpec : A
            {
                private ASpec()
                {
                }

                [StObjProperty]
                new public string StObjPower { get; set; }

                void Construct( IActivityLogger logger, B b )
                {
                    logger.Trace( "At ASpec level: StObjPower = '{0}'.", StObjPower );
                    TheB = b;
                }

                public B TheB { get; private set; }
            }

            public class B : IAmbientContract
            {
                void Construct( A a )
                {
                    TheA = a;
                }

                public A TheA { get; private set; }
            }

            class StObjPropertyConfigurator : IStObjStructuralConfigurator
            {
                public void Configure( IActivityLogger logger, IStObjMutableItem o )
                {
                    if( o.ObjectType == typeof( A ) ) o.SetStObjPropertyValue( logger, "StObjPower", "This is the A property." );
                    if( o.ObjectType == typeof( ASpec ) ) o.SetStObjPropertyValue( logger, "StObjPower", "ASpec level property." );
                }
            }

            public void DoTest()
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger, null, new StObjPropertyConfigurator() );
                collector.RegisterClass( typeof( B ) );
                collector.RegisterClass( typeof( ASpec ) );
                collector.DependencySorterHookInput = TestHelper.Trace;
                collector.DependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
                var r = collector.GetResult();
                {
                    Assert.That( r.HasFatalError, Is.False );

                    Assert.That( r.Default.StObjMap.Obtain<B>().TheA, Is.SameAs( r.Default.StObjMap.Obtain<A>() ).And.SameAs( r.Default.StObjMap.Obtain<ASpec>() ) );
                    Assert.That( r.Default.StObjMap.Obtain<ASpec>().TheB, Is.SameAs( r.Default.StObjMap.Obtain<B>() ) );
                    Assert.That( r.Default.StObjMap.ToStObj( typeof( A ) ).GetStObjProperty( "StObjPower" ), Is.EqualTo( "This is the A property." ) );
                    Assert.That( r.Default.StObjMap.ToStObj( typeof( ASpec ) ).GetStObjProperty( "StObjPower" ), Is.EqualTo( "ASpec level property." ) );

                    ASpec theA = (ASpec)r.Default.StObjMap.Obtain<A>();
                    Assert.That( theA.StObjPower, Is.EqualTo( "ASpec level property." ) );
                    Assert.That( typeof( A ).GetProperty( "StObjPower" ).GetValue( theA, null ), Is.EqualTo( "This is the A property." ) );
                }
                r.GenerateFinalAssembly( TestHelper.Logger, TestHelper.BinFolder, "TEST_ConstructCalled" );
                StObjContextRoot c = StObjContextRoot.Load( "TEST_ConstructCalled", TestHelper.Logger );
                {
                    Assert.That( c.Default.Obtain<B>().TheA, Is.SameAs( c.Default.Obtain<A>() ).And.SameAs( c.Default.Obtain<ASpec>() ) );
                    Assert.That( c.Default.Obtain<ASpec>().TheB, Is.SameAs( c.Default.Obtain<B>() ) );

                    ASpec theA = (ASpec)c.Default.Obtain<A>();
                    Assert.That( theA.StObjPower, Is.EqualTo( "ASpec level property." ) );
                    Assert.That( typeof( A ).GetProperty( "StObjPower" ).GetValue( theA, null ), Is.EqualTo( "This is the A property." ) );
                }
            }

        }

        [Test]
        public void ConstructCalledAndStObjProperties()
        {
            new CConstructCalledAndStObjProperties().DoTest();
        }
    }
}
