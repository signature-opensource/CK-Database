using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests.Service
{
    [TestFixture]
    public class InvalidServiceTests
    {
        [StObj( ItemKind = DependentItemKindSpec.Container )]
        class PackageA : IAmbientContract
        {
        }

        [StObj( ItemKind = DependentItemKindSpec.Container )]
        class Package1AfterA : IAmbientContract
        {
            void StObjConstruc( PackageA p ) { }
        }

        [StObj( ItemKind = DependentItemKindSpec.Container, Container = typeof( PackageA ) )]
        class Package2InsideA : IAmbientContract
        {
        }

        [AmbientServiceAttribute( typeof( PackageA ) )]
        class ServiceWith2Ctors : IAmbientService
        {
            public ServiceWith2Ctors()
            {
            }
            public ServiceWith2Ctors( int a )
            {
            }
        }

        [AmbientServiceAttribute( typeof( PackageA ) )]
        class ServiceWithDefaultCtor : IAmbientService
        {
        }

        [AmbientServiceAttribute( typeof( PackageA ) )]
        class ServiceWithOneCtor : IAmbientService
        {
            public ServiceWithOneCtor( int a )
            {
            }
        }

        [Test]
        public void services_must_have_one_and_only_one_public_ctor()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor );
                collector.RegisterType( typeof( ServiceWith2Ctors ) );
                collector.RegisteringFatalOrErrorCount.Should().Be( 1 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor );
                collector.RegisterType( typeof( ServiceWithOneCtor ) );
                collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Monitor );
                collector.RegisterType( typeof( ServiceWithDefaultCtor ) );
                collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            }
        }

        class ServiceWithoutAmbientServiceAttribute : IAmbientService
        {
        }

        [Test]
        public void AmbientServiceAttribute_is_required()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Monitor );
            collector.RegisterType( typeof( ServiceWithoutAmbientServiceAttribute ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 1 );
        }

        [AmbientService(typeof(PackageA))]
        class ServiceBase : IAmbientService
        {
            public ServiceBase( int a )
            {
            }
        }

        [AmbientService( typeof( PackageA ) )]
        class ServiceInherited : ServiceBase
        {
            public ServiceInherited()
                : base( 12 )
            {
            }
        }


        [Test]
        public void base_service_is_registered()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Monitor );
            collector.RegisterType( typeof( ServiceInherited ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 0 );
            var r = collector.GetResult( new SimpleServiceContainer() );
        }



    }
}
