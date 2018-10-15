using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests.Service.TypeCollector
{
    [TestFixture]
    public class ConstructorTests : TestsBase
    {
        [StObj( ItemKind = DependentItemKindSpec.Container )]
        class PackageA : IAmbientContract
        {
        }

        //[AmbientService( typeof( PackageA ) )]
        class ServiceWith2Ctors : IScopedAmbientService
        {
            public ServiceWith2Ctors()
            {
            }

            public ServiceWith2Ctors( int a )
            {
            }
        }


        //[AmbientService( typeof( PackageA ) )]
        class ServiceWithOneCtor : IScopedAmbientService
        {
            public ServiceWithOneCtor( int a )
            {
            }
        }

        //[AmbientService( typeof( PackageA ) )]
        class ServiceWithNonPublicCtor : IScopedAmbientService
        {
            internal ServiceWithNonPublicCtor( int a )
            {
            }
        }

        //[AmbientService( typeof( PackageA ) )]
        class ServiceWithDefaultCtor : IScopedAmbientService
        {
        }

        [Test]
        public void services_must_have_one_and_only_one_public_ctor()
        {
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClassOrPoco( typeof( PackageA ) );
                collector.RegisterClassOrPoco( typeof( ServiceWith2Ctors ) );
                CheckFailure( collector );
            }
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClassOrPoco( typeof( PackageA ) );
                collector.RegisterClassOrPoco( typeof( ServiceWithNonPublicCtor ) );
                CheckFailure( collector );
            }
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClassOrPoco( typeof( PackageA ) );
                collector.RegisterClassOrPoco( typeof( ServiceWithOneCtor ) );
                var r = CheckSuccess( collector );
                var c = r.AmbientServices.RootClasses.Single( x => x.Type == typeof( ServiceWithOneCtor ) );
                c.ConstructorInfo.Should().NotBeNull();
                var p = c.ConstructorParameters.Should().BeEmpty();
            }
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClassOrPoco( typeof( PackageA ) );
                collector.RegisterClassOrPoco( typeof( ServiceWithDefaultCtor ) );
                var r = CheckSuccess( collector );
                var c = r.AmbientServices.RootClasses.Single( x => x.Type == typeof( ServiceWithDefaultCtor ) );
                c.ConstructorInfo.Should().NotBeNull();
                c.ConstructorParameters.Should().BeEmpty();
            }
        }

        interface INotAnAmbientService
        {
        }

        interface ISNotRegistered : IScopedAmbientService
        {
        }

        interface ISRegistered : IScopedAmbientService
        {
        }

        class ServiceForISRegistered : ISRegistered
        {
        }

        class Consumer1Service : IScopedAmbientService
        {
            public Consumer1Service(
                INotAnAmbientService normal,
                ISNotRegistered notReg,
                ISRegistered reg )
            {
            }
        }

        [TestCase( "RegisteredDependentServiceButExcluded" )]
        [TestCase( "RegisteredDependentService" )]
        [TestCase( "NotRegistered" )]
        public void ctor_parameters_can_be_unregistered_services_interfaces_since_they_may_be_registered_at_runtime( string mode )
        {
            var collector = mode == "RegisteredDependentServiceButExcluded"
                            ? CreateAmbientTypeCollector( t => t != typeof( ServiceForISRegistered ) )
                            : CreateAmbientTypeCollector();

            if( mode != "NotRegistered" ) collector.RegisterClass( typeof( ServiceForISRegistered ) );
            collector.RegisterClass( typeof( Consumer1Service ) );
            var r = CheckSuccess( collector );
            var iRegistered = r.AmbientServices.LeafInterfaces.SingleOrDefault( x => x.Type == typeof( ISRegistered ) );
            if( mode == "RegisteredDependentService" )
            {
                iRegistered.Should().NotBeNull();
            }
            r.AmbientServices.RootClasses.Should().HaveCount( mode == "RegisteredDependentService" ? 2 : 1 );
            var c = r.AmbientServices.RootClasses.Single( x => x.Type == typeof( Consumer1Service ) );
            c.ConstructorInfo.Should().NotBeNull();
            if( mode == "RegisteredDependentService" )
            {
                c.ConstructorParameters.Should().HaveCount( 1 );
                c.ConstructorParameters[0].ParameterInfo.Name.Should().Be( "reg" );
                c.ConstructorParameters[0].ServiceClass.Should().BeNull();
                c.ConstructorParameters[0].ServiceInterface.Should().BeSameAs( iRegistered );
            }
            else
            {
                c.ConstructorParameters.Should().BeEmpty();
            }
        }

        class ConsumerWithClassDependencyService : IScopedAmbientService
        {
            public ConsumerWithClassDependencyService(
                INotAnAmbientService normal,
                ISNotRegistered notReg,
                ServiceForISRegistered classDependency )
            {
            }
        }

        class ConsumerWithDefaultService : IScopedAmbientService
        {
            public ConsumerWithDefaultService(
                INotAnAmbientService normal,
                ISNotRegistered notReg,
                ServiceForISRegistered classDependency = null )
            {
            }
        }

        [Test]
        public void ctor_parameters_cannot_be_unregistered_service_classe_unless_it_is_excluded_and_parameter_has_a_default_null()
        {
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClass( typeof( ServiceForISRegistered ) );
                collector.RegisterClass( typeof( ConsumerWithClassDependencyService ) );
                var r = CheckSuccess( collector );
                var dep = r.AmbientServices.RootClasses.Single( x => x.Type == typeof( ServiceForISRegistered ) );
                var c = r.AmbientServices.RootClasses.Single( x => x.Type == typeof( ConsumerWithClassDependencyService ) );
                c.ConstructorParameters.Should().HaveCount( 1, "'INotAnAmbientService normal' and 'ISNotRegistered notReg' are ignored." );
                c.ConstructorParameters[0].Position.Should().Be( 2 );
                c.ConstructorParameters[0].Name.Should().Be( "classDependency" );
                c.ConstructorParameters[0].ServiceClass.Should().BeSameAs( dep );
            }
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClass( typeof( ConsumerWithClassDependencyService ) );
                CheckFailure( collector );
            }
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClass( typeof( ConsumerWithDefaultService ) );
                CheckFailure( collector );
            }
            {
                var collector = CreateAmbientTypeCollector( t => t != typeof( ServiceForISRegistered ) );
                collector.RegisterClass( typeof( ServiceForISRegistered ) );
                collector.RegisterClass( typeof( ConsumerWithDefaultService ) );
                var r = CheckSuccess( collector );
                r.AmbientServices.RootClasses.Should().HaveCount( 1 );
                var c = r.AmbientServices.RootClasses.Single( x => x.Type == typeof( ConsumerWithDefaultService ) );
                c.ConstructorParameters.Should().BeEmpty();
            }

        }

        class AutoRef : IScopedAmbientService
        {
            public AutoRef( AutoRef a )
            {
            }
        }

        class RefBased : IScopedAmbientService
        {
        }

        class BaseReferencer : RefBased
        {
            public BaseReferencer( RefBased b )
            {
            }
        }

        class RefIntermediate : RefBased { }

        class RefIntermediate2 : RefIntermediate
        {
            public RefIntermediate2( RefBased b )
            {
            }
        }


        [Test]
        public void no_constructor_parameter_super_type_rule()
        {
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClassOrPoco( typeof( AutoRef ) );
                CheckFailure( collector );
            }

            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClassOrPoco( typeof( BaseReferencer ) );
                CheckFailure( collector );
            }

            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClassOrPoco( typeof( RefIntermediate2 ) );
                CheckFailure( collector );
            }

        }

        class StupidA : IScopedAmbientService
        {
            public StupidA( SpecializedStupidA child )
            {
            }
        }

        class SpecializedStupidA : StupidA
        {
            public SpecializedStupidA()
                : base( null )
            {
            }
        }

        [Test]
        public void stupid_loop()
        {
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClassOrPoco( typeof( SpecializedStupidA ) );
                CheckFailure( collector );
            }
        }

    }
}
