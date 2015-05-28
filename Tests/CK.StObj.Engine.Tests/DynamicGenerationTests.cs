#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\DynamicGenerationTests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
                public bool Implement( IActivityMonitor monitor, System.Reflection.MethodInfo m, IDynamicAssembly dynamicAssembly, System.Reflection.Emit.TypeBuilder b, bool isVirtual )
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

                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor, runtimeBuilder: runtimeBuilder );
                collector.RegisterClass( typeof( B ) );
                collector.RegisterClass( typeof( D ) );
                collector.DependencySorterHookInput = items => TestHelper.ConsoleMonitor.TraceDependentItem( items );
                collector.DependencySorterHookOutput = sortedItems => TestHelper.ConsoleMonitor.TraceSortedItem( sortedItems, false );
                var r = collector.GetResult();
                Assert.That( r.HasFatalError, Is.False );
                // Null as directory => use CK.StObj.Model folder.
                r.GenerateFinalAssembly( TestHelper.ConsoleMonitor, StObjContextRoot.DefaultStObjRuntimeBuilder, BuilderFinalAssemblyConfiguration.GenerateOption.GenerateFile, null, "TEST_SimpleEmit" );

                IStObjMap c = StObjContextRoot.Load( "TEST_SimpleEmit", runtimeBuilder, TestHelper.ConsoleMonitor );
                Assert.That( typeof( B ).IsAssignableFrom( c.Default.ToLeafType( typeof( A ) ) ) );
                Assert.That( c.Default.ToLeafType( typeof( IC ) ), Is.SameAs( typeof( D ) ) );
                Assert.That( c.Default.Obtain<B>().Auto(3), Is.EqualTo( 0 ) );
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

                void Construct( IActivityMonitor monitor )
                {
                    monitor.Trace().Send( "At A level: StObjPower = '{0}'.", StObjPower );
                }
            }

            public class ASpec : A
            {
                [StObjProperty]
                new public string StObjPower { get; set; }

                void Construct( IActivityMonitor monitor, B b )
                {
                    monitor.Trace().Send( "At ASpec level: StObjPower = '{0}'.", StObjPower );
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
                public void Configure( IActivityMonitor monitor, IStObjMutableItem o )
                {
                    if( o.ObjectType == typeof( A ) ) o.SetStObjPropertyValue( monitor, "StObjPower", "This is the A property." );
                    if( o.ObjectType == typeof( ASpec ) ) o.SetStObjPropertyValue( monitor, "StObjPower", "ASpec level property." );
                }
            }

            public void DoTest()
            {
                StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor, configurator: new StObjPropertyConfigurator() );
                collector.RegisterClass( typeof( B ) );
                collector.RegisterClass( typeof( ASpec ) );
                collector.DependencySorterHookInput = items => TestHelper.ConsoleMonitor.TraceDependentItem( items );
                collector.DependencySorterHookOutput = sortedItems => TestHelper.ConsoleMonitor.TraceSortedItem( sortedItems, false );
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

                r.GenerateFinalAssembly( TestHelper.ConsoleMonitor, StObjContextRoot.DefaultStObjRuntimeBuilder, BuilderFinalAssemblyConfiguration.GenerateOption.GenerateFile, TestHelper.BinFolder, "TEST_ConstructCalled" );

                IStObjMap c = StObjContextRoot.Load( "TEST_ConstructCalled", StObjContextRoot.DefaultStObjRuntimeBuilder, TestHelper.ConsoleMonitor );
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
