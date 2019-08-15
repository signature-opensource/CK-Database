using CK.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;

namespace CK.StObj.Engine.Tests.Service.StObj
{
    namespace Local
    {
        [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
        class ReplaceAmbientServiceAttribute : Attribute
        {
            public ReplaceAmbientServiceAttribute( string replacedAssemblyQualifiedName )
            {
            }
        }
    }

    [TestFixture]
    public class ServiceTests : TestsBase
    {
        public interface ISampleService : IAmbientService
        {
        }

        public class SampleService : ISampleService
        {
        }

        [ReplaceAmbientService( typeof( SampleService ) )]
        public class SampleService2 : ISampleService
        {
        }


        [Test]
        public void ReplaceAmbientService_works_with_type()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( SampleService ) );
            collector.RegisterType( typeof( SampleService2 ) );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( ISampleService )].ClassType.Should().Be( typeof( SampleService2 ) );
            r.Services.SimpleMappings[typeof( SampleService )].ClassType.Should().Be( typeof( SampleService ) );
        }

        [Local.ReplaceAmbientService( "CK.StObj.Engine.Tests.Service.StObj.ServiceTests+SampleService2, CK.StObj.Engine.Tests" )]
        public class SampleService3 : ISampleService
        {
        }

        [Test]
        public void ReplaceAmbientService_works_with_assembly_qualified_name_and_locally_defined_attribute()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( SampleService ) );
            collector.RegisterType( typeof( SampleService2 ) );
            collector.RegisterType( typeof( SampleService3 ) );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( ISampleService )].ClassType.Should().Be( typeof( SampleService3 ) );
            r.Services.SimpleMappings[typeof( SampleService2 )].ClassType.Should().Be( typeof( SampleService2 ) );
            r.Services.SimpleMappings[typeof( SampleService )].ClassType.Should().Be( typeof( SampleService ) );
        }


        public class UseActivityMonitor : ISingletonAmbientService
        {
            public UseActivityMonitor( IActivityMonitor m )
            {
            }
        }

        [Test]
        public void IActivityMonitor_is_Scoped_by_default()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( UseActivityMonitor ) );
            CheckFailure( collector );
        }

        public class Obj : IAmbientObject, ISampleService
        {
        }


        public interface IInvalidInterface : IAmbientObject, ISampleService
        {
        }

        [Test]
        public void an_AmbientObject_class_can_be_an_IAmbientService_but_an_interface_cannot()
        {
            {
                var collector = CreateStObjCollector();
                collector.RegisterType( typeof( Obj ) );
                var (collectorResult, map, sp) = CheckSuccessAndBuildServices( collector );
                // On runtime data.
                collectorResult.Services.ObjectMappings[typeof( ISampleService )].Should().BeOfType<Obj>();
                collectorResult.StObjs.Obtain<Obj>().Should().BeOfType<Obj>();
                collectorResult.StObjs.Obtain<ISampleService>().Should().BeNull( "ISampleService is a Service." );
                // On generated data.
                map.Services.ObjectMappings[typeof( ISampleService )].Should().BeOfType<Obj>();
                map.StObjs.Obtain<Obj>().Should().BeOfType<Obj>();
                map.StObjs.Obtain<ISampleService>().Should().BeNull( "ISampleService is a Service." );
                var o = sp.GetRequiredService<Obj>();
                sp.GetRequiredService<ISampleService>().Should().BeSameAs( o );
            }
            {
                var collector = CreateStObjCollector();
                collector.RegisterType( typeof( IInvalidInterface ) );
                CheckFailure( collector );
            }
        }

        public abstract class ObjSpec : Obj
        {
        }

        [Test]
        public void an_AmbientObject_class_and_IAmbientService_with_specialization()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( ObjSpec ) );
            var (collectorResult, map, sp) = CheckSuccessAndBuildServices( collector );
            // On runtime data.
            collectorResult.Services.ObjectMappings[typeof( ISampleService )].Should().BeAssignableTo<ObjSpec>();
            collectorResult.StObjs.Obtain<ISampleService>().Should().BeNull( "ISampleService is a Service." );
            collectorResult.StObjs.Obtain<Obj>().Should().BeAssignableTo<ObjSpec>();
            collectorResult.StObjs.Obtain<ObjSpec>().Should().BeAssignableTo<ObjSpec>();
            // On generated data.
            map.Services.ObjectMappings[typeof( ISampleService )].Should().BeAssignableTo<ObjSpec>();
            map.StObjs.Obtain<ISampleService>().Should().BeNull( "ISampleService is a Service." );
            map.StObjs.Obtain<Obj>().Should().BeAssignableTo<ObjSpec>();
            map.StObjs.Obtain<ObjSpec>().Should().BeAssignableTo<ObjSpec>();
            var o = sp.GetRequiredService<Obj>();
            sp.GetRequiredService<ObjSpec>().Should().BeSameAs( o );
            sp.GetRequiredService<ISampleService>().Should().BeSameAs( o );
        }

        public interface ISampleServiceSpec : ISampleService
        {
        }

        // Intermediate concrete class: this doesn't change anything.
        public class ObjSpecIntermediate : ObjSpec, ISampleServiceSpec
        {
        }

        public abstract class ObjSpecFinal : ObjSpecIntermediate
        {
        }

        [Test]
        public void an_AmbientObject_class_and_IAmbientService_with_deep_specializations()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( ObjSpecFinal ) );
            var (collectorResult, map, sp) = CheckSuccessAndBuildServices( collector );
            // On runtime data.
            collectorResult.Services.ObjectMappings[typeof( ISampleService )].Should().BeAssignableTo<ObjSpecFinal>();
            collectorResult.Services.ObjectMappings[typeof( ISampleServiceSpec )].Should().BeAssignableTo<ObjSpecFinal>();
            collectorResult.StObjs.Obtain<ISampleService>().Should().BeNull( "ISampleService is a Service." );
            collectorResult.StObjs.Obtain<ISampleServiceSpec>().Should().BeNull( "ISampleServiceSpec is a Service." );
            collectorResult.StObjs.Obtain<Obj>().Should().BeAssignableTo<ObjSpecFinal>();
            collectorResult.StObjs.Obtain<ObjSpec>().Should().BeAssignableTo<ObjSpecFinal>();
            collectorResult.StObjs.Obtain<ObjSpecIntermediate>().Should().BeAssignableTo<ObjSpecFinal>();
            // On generated data.
            map.Services.ObjectMappings[typeof( ISampleService )].Should().BeAssignableTo<ObjSpecFinal>();
            map.Services.ObjectMappings[typeof( ISampleServiceSpec )].Should().BeAssignableTo<ObjSpecFinal>();
            map.StObjs.Obtain<ISampleService>().Should().BeNull( "ISampleService is a Service." );
            map.StObjs.Obtain<ISampleServiceSpec>().Should().BeNull( "ISampleServiceSpec is a Service." );
            map.StObjs.Obtain<Obj>().Should().BeAssignableTo<ObjSpecFinal>();
            map.StObjs.Obtain<ObjSpec>().Should().BeAssignableTo<ObjSpecFinal>();
            map.StObjs.Obtain<ObjSpecIntermediate>().Should().BeAssignableTo<ObjSpecFinal>();
            var o = sp.GetRequiredService<ObjSpecFinal>();
            sp.GetRequiredService<Obj>().Should().BeSameAs( o );
            sp.GetRequiredService<ObjSpec>().Should().BeSameAs( o );
            sp.GetRequiredService<ObjSpecIntermediate>().Should().BeSameAs( o );
            sp.GetRequiredService<ISampleService>().Should().BeSameAs( o );
            sp.GetRequiredService<ISampleServiceSpec>().Should().BeSameAs( o );
        }


    }
}
