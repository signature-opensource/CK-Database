using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.Setup;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Collections;
using CK.CodeGen;
using CK.CodeGen.Abstractions;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    [Category( "DynamicGeneration" )]
    public class DynamicGenerationTests
    {
        public class CSimpleEmit
        {

            class AutoImplementedAttribute : Attribute, IAutoImplementorMethod
            {
                public bool Implement(IActivityMonitor monitor, MethodInfo m, IDynamicAssembly dynamicAssembly, ITypeScope b)
                {
                    b.AppendOverrideSignature( m )
                     .Append( $"=> default({m.ReturnType.FullName});" )
                     .NewLine();
                    return true;
                }
            }

            public class A : IAmbientContract
            {
            }

            public abstract class B : A
            {
                readonly string _str;

                protected B( string injectableCtor )
                {
                    _str = injectableCtor;
                }

                public string InjectedString 
                { 
                    get { return _str; } 
                }

                [AutoImplemented]
                public abstract int Auto( int i );
            }

            public interface IC : IAmbientContract
            {
                A TheA { get; }
            }

            public class C : IC
            {
                [InjectContract]
                public A TheA { get; private set; }
            }

            public class D : C
            {
                [AmbientProperty( IsOptional = true )]
                public string AnOptionalString { get; private set; }
            }
            
            const string ctorParam = "Protected Ctor is called by public's finalType's constructor.";

            class StObjRuntimeBuilder : IStObjRuntimeBuilder
            {
                public object CreateInstance( Type finalType )
                {
                    if( typeof( B ).IsAssignableFrom( finalType ) ) return Activator.CreateInstance( finalType, ctorParam );
                    else return Activator.CreateInstance( finalType, false );
                }
            }

            public void DoTest()
            {
                var runtimeBuilder = new StObjRuntimeBuilder();


                StObjCollector collector = new StObjCollector( TestHelper.Monitor, runtimeBuilder: runtimeBuilder );
                collector.RegisterType( typeof( B ) );
                collector.RegisterType( typeof( D ) );
                collector.DependencySorterHookInput = items => TestHelper.Monitor.TraceDependentItem( items );
                collector.DependencySorterHookOutput = sortedItems => TestHelper.Monitor.TraceSortedItem( sortedItems, false );
                var r = collector.GetResult( new SimpleServiceContainer() );
                Assert.That( r.HasFatalError, Is.False );

                r.GenerateFinalAssembly( TestHelper.Monitor, Path.Combine( AppContext.BaseDirectory, "TEST_SimpleEmit.dll" ), false, null );
                var a = TestHelper.LoadAssemblyFromAppContextBaseDirectory( "TEST_SimpleEmit" );
                IStObjMap c = StObjContextRoot.Load( a, runtimeBuilder, TestHelper.Monitor );
                Assert.That( typeof( B ).IsAssignableFrom( c.Default.ToLeafType( typeof( A ) ) ) );
                Assert.That( c.Default.ToLeafType( typeof( IC ) ), Is.SameAs( typeof( D ) ) );
                Assert.That( c.Default.Obtain<B>().Auto( 3 ), Is.EqualTo( 0 ) );
                Assert.That( c.Default.Obtain<B>().InjectedString, Is.EqualTo( ctorParam ) );
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

                void StObjConstruct( IActivityMonitor monitor )
                {
                    monitor.Trace( $"At A level: StObjPower = '{StObjPower}'." );
                }
            }

            public abstract class ASpec : A
            {
                [StObjProperty]
                new public string StObjPower { get; set; }

                void StObjConstruct( IActivityMonitor monitor, B b )
                {
                    monitor.Trace( $"At ASpec level: StObjPower = '{StObjPower}'." );
                    TheB = b;
                }

                public B TheB { get; private set; }
            }

            public class B : IAmbientContract
            {
                void StObjConstruct( A a )
                {
                    TheA = a;
                }

                public A TheA { get; private set; }
            }

            class StObjPropertyConfigurator : IStObjStructuralConfigurator
            {
                public void Configure( IActivityMonitor monitor, IStObjMutableItem o )
                {
                    if( o.ObjectType == typeof( A ) ) o.SetStObjPropertyValue( monitor, "StObjPower", "This is the A property." );
                    if( o.ObjectType == typeof( ASpec ) ) o.SetStObjPropertyValue( monitor, "StObjPower", "ASpec level property." );
                }
            }

            public void DoTest()
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, configurator: new StObjPropertyConfigurator() );
                collector.RegisterType( typeof( B ) );
                collector.RegisterType( typeof( ASpec ) );
                collector.DependencySorterHookInput = items => TestHelper.Monitor.TraceDependentItem( items );
                collector.DependencySorterHookOutput = sortedItems => TestHelper.Monitor.TraceSortedItem( sortedItems, false );
                var r = collector.GetResult( new SimpleServiceContainer() );
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

                r.GenerateFinalAssembly( TestHelper.Monitor, Path.Combine( AppContext.BaseDirectory, "TEST_ConstructCalled.dll" ), false, null );
                {
                    var a = TestHelper.LoadAssemblyFromAppContextBaseDirectory( "TEST_ConstructCalled" );
                    IStObjMap c = StObjContextRoot.Load( a, StObjContextRoot.DefaultStObjRuntimeBuilder, TestHelper.Monitor );
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

        public class PostBuildSet
        {
            public class A : IAmbientContract
            {
                [StObjProperty]
                public string StObjPower { get; set; }

                public bool StObjInitializeOnACalled; 

                void StObjConstruct( IActivityMonitor monitor, [Container]BSpec bIsTheContainerOfA )
                {
                    monitor.Trace( $"At A level: StObjPower = '{StObjPower}'." );
                }

                void StObjInitialize( IActivityMonitor monitor, IStObjObjectMap map )
                {
                    Assert.That( map.Implementations.OfType<IAmbientContract>().Count, Is.EqualTo( 2 ) );
                    StObjInitializeOnACalled = true;
                }

                [InjectContract]
                public BSpec TheB { get; private set; }
            }

            public abstract class ASpec : A
            {
                [StObjProperty]
                new public string StObjPower { get; set; }

                public bool StObjInitializeOnASpecCalled;

                void StObjConstruct( IActivityMonitor monitor )
                {
                    monitor.Trace( $"At ASpec level: StObjPower = '{StObjPower}'." );
                }

                void StObjInitialize( IActivityMonitor monitor, IStObjObjectMap map )
                {
                    Assert.That( map.Implementations.OfType<IAmbientContract>().Count, Is.EqualTo( 2 ) );
                    Assert.That( StObjInitializeOnACalled );
                    StObjInitializeOnASpecCalled = true;
                }

            }

            [StObj( ItemKind = DependentItemKindSpec.Container )]
            public class B : IAmbientContract
            {
                [InjectContract]
                public A TheA { get; private set; }

                [InjectContract]
                public A TheInjectedA { get; private set; }
            }

            public abstract class BSpec : B
            {
                void StObjConstruct( )
                {
                }
            }

            class StObjPropertyConfigurator : IStObjStructuralConfigurator
            {
                public void Configure( IActivityMonitor monitor, IStObjMutableItem o )
                {
                    if( o.ObjectType == typeof( A ) ) o.SetStObjPropertyValue( monitor, "StObjPower", "This is the A property." );
                    if( o.ObjectType == typeof( ASpec ) ) o.SetStObjPropertyValue( monitor, "StObjPower", "ASpec level property." );
                }
            }

            public void DoTest()
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor, configurator: new StObjPropertyConfigurator() );
                collector.RegisterType( typeof( BSpec ) );
                collector.RegisterType( typeof( ASpec ) );
                collector.DependencySorterHookInput = items => TestHelper.Monitor.TraceDependentItem( items );
                collector.DependencySorterHookOutput = sortedItems => TestHelper.Monitor.TraceSortedItem( sortedItems, false );
                var r = collector.GetResult( new SimpleServiceContainer() );
                {
                    Assert.That( r.HasFatalError, Is.False );

                    Assert.That( r.Default.StObjMap.Obtain<B>().TheA, Is.SameAs( r.Default.StObjMap.Obtain<A>() ).And.SameAs( r.Default.StObjMap.Obtain<ASpec>() ) );
                    Assert.That( r.Default.StObjMap.Obtain<ASpec>().TheB, Is.SameAs( r.Default.StObjMap.Obtain<B>() ) );
                    Assert.That( r.Default.StObjMap.ToStObj( typeof( A ) ).GetStObjProperty( "StObjPower" ), Is.EqualTo( "This is the A property." ) );
                    Assert.That( r.Default.StObjMap.ToStObj( typeof( ASpec ) ).GetStObjProperty( "StObjPower" ), Is.EqualTo( "ASpec level property." ) );

                    ASpec theA = (ASpec)r.Default.StObjMap.Obtain<A>();
                    Assert.That( theA.StObjPower, Is.EqualTo( "ASpec level property." ) );
                    Assert.That( typeof( A ).GetProperty( "StObjPower" ).GetValue( theA, null ), Is.EqualTo( "This is the A property." ) );
                    Assert.That( theA.StObjInitializeOnACalled, Is.False, "StObjInitialize is NOT called on temporary instances." );
                }

                r.GenerateFinalAssembly( TestHelper.Monitor, Path.Combine( AppContext.BaseDirectory, "TEST_PostBuildSet.dll" ), false, null );

                {
                    var a = TestHelper.LoadAssemblyFromAppContextBaseDirectory( "TEST_PostBuildSet" );
                    IStObjMap c = StObjContextRoot.Load( a, StObjContextRoot.DefaultStObjRuntimeBuilder, TestHelper.Monitor );
                    Assert.That( c.Default.Obtain<B>().TheA, Is.SameAs( c.Default.Obtain<A>() ).And.SameAs( c.Default.Obtain<ASpec>() ) );
                    Assert.That( c.Default.Obtain<ASpec>().TheB, Is.SameAs( c.Default.Obtain<B>() ) );

                    ASpec theA = (ASpec)c.Default.Obtain<A>();
                    Assert.That( theA.StObjPower, Is.EqualTo( "ASpec level property." ) );
                    Assert.That( typeof( A ).GetProperty( "StObjPower" ).GetValue( theA, null ), Is.EqualTo( "This is the A property." ) );

                    Assert.That( theA.TheB, Is.SameAs( c.Default.Obtain<B>() ) );
                    Assert.That( c.Default.Obtain<B>().TheInjectedA, Is.SameAs( theA ) );

                    Assert.That( theA.StObjInitializeOnACalled, Is.True );
                    Assert.That( theA.StObjInitializeOnASpecCalled, Is.True );
                }
            }

        }

        [Test]
        public void PostBuildAndAmbientContracts()
        {
            new PostBuildSet().DoTest();
        }



    }
}
