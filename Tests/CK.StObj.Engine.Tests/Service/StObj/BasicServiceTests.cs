using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests.Service.StObj
{
    [TestFixture]
    public class BasicServiceTests : TestsBase
    {
        interface IServiceRegistered : IScopedAmbientService
        {
        }

        [Test]
        public void only_IPoco_or_classes_can_be_registered()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( IServiceRegistered ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 1 );
            CheckFailure( collector );
        }


        interface IAmbientService { }
        interface IScopedAmbientService { }
        interface ISingletonAmbientService { }

        // These SimpleClass are tagged with locally named interfaces.
        class SimpleClassSingleton : ISingletonAmbientService { }
        class SimpleClassScoped : IScopedAmbientService { }
        class SimpleClassAmbient : IAmbientService { }

        [Test]
        public void class_scope_simple_tests()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( SimpleClassSingleton ) );
            collector.RegisterType( typeof( SimpleClassScoped ) );
            collector.RegisterType( typeof( SimpleClassAmbient ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( SimpleClassSingleton )].IsScoped.Should().BeFalse();
            r.Services.SimpleMappings[typeof( SimpleClassScoped )].IsScoped.Should().BeTrue();
            r.Services.SimpleMappings[typeof( SimpleClassAmbient )].IsScoped.Should().BeFalse();
        }

        class BuggyDoubleScopeClassAmbient : IScopedAmbientService, Core.ISingletonAmbientService { }

        [Test]
        public void a_class_with_both_scopes_is_an_error()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( BuggyDoubleScopeClassAmbient ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 1 );
            CheckFailure( collector );
        }

        class LifetimeErrorClassAmbientBecauseOfScoped : Core.ISingletonAmbientService
        {
            public LifetimeErrorClassAmbientBecauseOfScoped( SimpleClassScoped d )
            {
            }
        }

        [Test]
        public void a_singleton_that_depends_on_scoped_is_an_error()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( SimpleClassScoped ) );
            collector.RegisterType( typeof( LifetimeErrorClassAmbientBecauseOfScoped ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            CheckFailure( collector );
        }

        interface IExternalService { }

        class LifetimeOfExternalBoostToSingleton : Core.ISingletonAmbientService
        {
            public LifetimeOfExternalBoostToSingleton( IExternalService e )
            {
            }
        }

        [Test]
        public void a_singleton_that_depends_on_an_unknwon_external_defines_the_external_as_singletons()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( LifetimeOfExternalBoostToSingleton ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            var r = CheckSuccess( collector );
            r.Services.ExternallyDefinedSingletons.Contains( typeof( IExternalService ) );
        }

        [Test]
        public void a_singleton_that_depends_on_external_that_is_defined_as_a_singleton_is_fine()
        {
            var collector = CreateStObjCollector();
            collector.DefineAsExternalSingletons( new[] { typeof( IExternalService) } );
            collector.RegisterType( typeof( LifetimeOfExternalBoostToSingleton ) );
            CheckSuccess( collector );
        }

        class SingletonThatDependsOnSingleton : Core.ISingletonAmbientService
        {
            public SingletonThatDependsOnSingleton( SimpleClassSingleton e )
            {
            }
        }

        [Test]
        public void a_singleton_that_depends_on_singleton()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( SimpleClassSingleton ) );
            collector.RegisterType( typeof( SingletonThatDependsOnSingleton ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            CheckSuccess( collector );
        }

        class AmbientThatDependsOnSingleton : IAmbientService
        {
            public AmbientThatDependsOnSingleton( SimpleClassSingleton e )
            {
            }
        }

        [Test]
        public void an_ambient_service_that_depends_only_on_singleton_is_singleton()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( SimpleClassSingleton ) );
            collector.RegisterType( typeof( AmbientThatDependsOnSingleton ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( AmbientThatDependsOnSingleton )].IsScoped.Should().BeFalse();
        }

        interface IAmbientThatDependsOnNothing : IAmbientService { }

        class AmbientThatDependsOnNothing : IAmbientThatDependsOnNothing { }

        [Test]
        public void an_ambient_service_that_depends_on_nothing_is_singleton()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( AmbientThatDependsOnNothing ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( IAmbientThatDependsOnNothing )].IsScoped.Should().BeFalse();
            r.Services.SimpleMappings[typeof( AmbientThatDependsOnNothing )].IsScoped.Should().BeFalse();
        }

        class AmbientThatDependsOnExternal : IAmbientService
        {
            public AmbientThatDependsOnExternal( IExternalService e )
            {
            }
        }

        [Test]
        public void an_ambient_service_that_depends_on_an_external_service_is_Scoped()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( AmbientThatDependsOnExternal ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( AmbientThatDependsOnExternal )].IsScoped.Should().BeTrue();
        }

        interface ISampleAmbientContract : IAmbientObject { }

        class SampleAmbientContract : ISampleAmbientContract { }

        interface ISamplePoco : IPoco { }

        class AmbientThatWillBeResolvedAsSingleton : IAmbientService
        {
            public AmbientThatWillBeResolvedAsSingleton( ISampleAmbientContract c )
            {
            }
        }


        class AmbientThatDependsOnAllKindOfSingleton : IAmbientService
        {
            public AmbientThatDependsOnAllKindOfSingleton(
                IExternalService e,
                IPocoFactory<ISamplePoco> pocoFactory,
                ISampleAmbientContract contract,
                IAmbientThatDependsOnNothing ambientThatDependsOnNothing,
                AmbientThatDependsOnSingleton d,
                SimpleClassSingleton s,
                AmbientThatWillBeResolvedAsSingleton other )
            {
            }
        }

        [Test]
        public void an_ambient_service_that_depends_on_all_kind_of_singletons_is_singleton()
        {
            var collector = CreateStObjCollector();
            collector.DefineAsExternalSingletons( new[] { typeof( IExternalService ) } );
            collector.RegisterType( typeof( AmbientThatDependsOnAllKindOfSingleton ) );
            collector.RegisterType( typeof( AmbientThatDependsOnExternal ) );
            collector.RegisterType( typeof( SampleAmbientContract ) );
            collector.RegisterType( typeof( AmbientThatDependsOnSingleton ) );
            collector.RegisterType( typeof( SimpleClassSingleton ) );
            collector.RegisterType( typeof( AmbientThatWillBeResolvedAsSingleton ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( AmbientThatDependsOnAllKindOfSingleton )].IsScoped.Should().BeFalse();
        }

        interface IOtherExternalService { }

        class AmbientThatDependsOnAnotherExternalService : IAmbientService
        {
            public AmbientThatDependsOnAnotherExternalService( IOtherExternalService o )
            {
            }
        }

        class AmbientThatDependsOnAllKindOfSingletonAndAnOtherExternalService : IAmbientService
        {
            public AmbientThatDependsOnAllKindOfSingletonAndAnOtherExternalService(
                IExternalService e,
                IPocoFactory<ISamplePoco> pocoFactory,
                ISampleAmbientContract contract,
                IAmbientThatDependsOnNothing ambientThatDependsOnNothing,
                AmbientThatDependsOnSingleton d,
                SimpleClassSingleton s,
                AmbientThatDependsOnAnotherExternalService o )
            {
            }
        }

        [TestCase( "UnknwonLifetimeExternalService" )]
        [TestCase( "WithSingletonLifetimeOnExternalService" )]
        public void a_singleton_service_that_depends_on_all_kind_of_singletons_is_singleton( string mode )
        {
            var collector = CreateStObjCollector();
            collector.DefineAsExternalSingletons( new[] { typeof( IExternalService ) } );
            collector.RegisterType( typeof( AmbientThatDependsOnAllKindOfSingletonAndAnOtherExternalService ) );
            collector.RegisterType( typeof( AmbientThatDependsOnExternal ) );
            collector.RegisterType( typeof( SampleAmbientContract ) );
            collector.RegisterType( typeof( AmbientThatDependsOnSingleton ) );
            collector.RegisterType( typeof( SimpleClassSingleton ) );
            collector.RegisterType( typeof( AmbientThatDependsOnAnotherExternalService ) );
            if( mode == "WithSingletonLifetimeOnExternalService" )
            {
                collector.DefineAsExternalSingletons( new[] { typeof( IOtherExternalService ) } );
            }
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            var r = CheckSuccess( collector );
            bool isScoped = r.Services.SimpleMappings[typeof( AmbientThatDependsOnAllKindOfSingletonAndAnOtherExternalService )].IsScoped;
            isScoped.Should().Be( mode == "UnknwonLifetimeExternalService" );
        }

    }
}
