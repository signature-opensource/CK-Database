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
    public class ServiceSimpleMappingTests : TestsBase
    {
        public interface ISBase : IScopedAmbientService
        {
        }

        interface IS1 : ISBase
        {
        }

        interface IS2 : ISBase
        {
        }

        class ServiceS1Impl : IS1
        {
        }

        class ServiceS2Impl : IS2
        {
        }

        class ServiceS1S2Impl : IS1, IS2
        {
        }


        [Test]
        public void service_interfaces_requires_unification_otherwise_ISBase_would_be_ambiguous()
        {
            {
                var collector = CreateStObjCollector();
                collector.RegisterType( typeof( ServiceS1Impl ) );
                collector.RegisterType( typeof( ServiceS2Impl ) );
                var r = CheckFailure( collector );
                r.AmbientTypeResult.AmbientServices.RootClasses.Should().HaveCount( 2 );
            }
            // Same tests as above but excluding ISBase type: success since
            // ISBase is no more considered a IScopedAmbientService.
            {
                var collector = CreateStObjCollector( t => t != typeof( ISBase ) );
                collector.RegisterType( typeof( ServiceS1Impl ) );
                collector.RegisterType( typeof( ServiceS2Impl ) );
                var r = CheckSuccess( collector );
                r.Services.SimpleMappings.ContainsKey( typeof( ISBase ) ).Should().BeFalse();
                r.Services.SimpleMappings[typeof( IS1 )].Should().BeSameAs( typeof( ServiceS1Impl ) );
                r.Services.SimpleMappings[typeof( IS2 )].Should().BeSameAs( typeof( ServiceS2Impl ) );
                r.Services.SimpleMappings[typeof( ServiceS1Impl )].Should().BeSameAs( typeof( ServiceS1Impl ) );
                r.Services.SimpleMappings[typeof( ServiceS2Impl )].Should().BeSameAs( typeof( ServiceS2Impl ) );
            }
        }

        [Test]
        public void service_interfaces_with_single_implementation()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( ServiceS1S2Impl ) );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( ISBase )].Should().BeSameAs( typeof( ServiceS1S2Impl ) );
            r.Services.SimpleMappings[typeof( IS2 )].Should().BeSameAs( typeof( ServiceS1S2Impl ) );
            r.Services.SimpleMappings[typeof( IS1 )].Should().BeSameAs( typeof( ServiceS1S2Impl ) );
            r.Services.SimpleMappings[typeof( ServiceS1S2Impl )].Should().BeSameAs( typeof( ServiceS1S2Impl ) );
        }

        interface ISU : IS1, IS2
        {
        }

        class ServiceUnifiedImpl : ISU
        {
        }

        [Test]
        public void service_interfaces_unification_works()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( ServiceUnifiedImpl ) );
            var r = CheckSuccess( collector );
            var interfaces = r.AmbientTypeResult.AmbientServices.LeafInterfaces;
            interfaces.Should().HaveCount( 1 );
            var iSU = interfaces[0];
            iSU.Type.Should().Be( typeof( ISU ) );
            iSU.Interfaces.Select( i => i.Type ).Should().BeEquivalentTo( typeof(ISBase), typeof(IS1), typeof(IS2) );
            r.AmbientTypeResult.AmbientServices.RootClasses.Should().ContainSingle().And.Contain( c => c.Type == typeof( ServiceUnifiedImpl ) );
            r.Services.SimpleMappings[typeof( ISU )].Should().BeSameAs( typeof( ServiceUnifiedImpl ) );
            r.Services.SimpleMappings[typeof( IS1 )].Should().BeSameAs( typeof( ServiceUnifiedImpl ) );
            r.Services.SimpleMappings[typeof( IS2 )].Should().BeSameAs( typeof( ServiceUnifiedImpl ) );
            r.Services.SimpleMappings[typeof( ISBase )].Should().BeSameAs( typeof( ServiceUnifiedImpl ) );
        }

        interface IMultiImplService : IScopedAmbientService
        {
        }

        // Intermediate class.
        class ServiceImplBaseBase : IMultiImplService
        {
        }

        // Root class with 2 ambiguous specializations (ServiceImpl1 and ServiceImpl3).
        class ServiceImplRootProblem : ServiceImplBaseBase
        {
        }

        // First ambiguous class.
        class ServiceImpl1 : ServiceImplRootProblem
        {
        }

        // Intermediate class.
        class ServiceImpl2 : ServiceImplRootProblem
        {
        }

        // Second ambiguous class.
        class ServiceImpl3 : ServiceImpl2
        {
        }

        // Solver (uses Class Unification).
        class ResolveByClassUnification : ServiceImpl3
        {
            public ResolveByClassUnification( ServiceImpl1 s1 )
            {
            }
        }

        [TestCase( "With Service Chaining" )]
        [TestCase( "Without" )]
        public void service_classes_ambiguities_that_requires_Service_Chaining( string mode )
        {
            bool solved = mode == "With Service Chaining";

            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( ServiceImpl1 ) );
            collector.RegisterType( typeof( ServiceImpl3 ) );
            if( solved ) collector.RegisterType( typeof( ResolveByClassUnification ) );

            if( solved )
            {
                var r = CheckSuccess( collector );
                r.Services.SimpleMappings[typeof( IMultiImplService )].Should().BeSameAs( typeof( ResolveByClassUnification ) );
                r.Services.SimpleMappings[typeof( ServiceImplBaseBase )].Should().BeSameAs( typeof( ResolveByClassUnification ) );
                r.Services.SimpleMappings[typeof( ServiceImplRootProblem )].Should().BeSameAs( typeof( ResolveByClassUnification ) );
                r.Services.SimpleMappings[typeof( ServiceImpl2 )].Should().BeSameAs( typeof( ResolveByClassUnification ) );
                r.Services.SimpleMappings[typeof( ServiceImpl3 )].Should().BeSameAs( typeof( ResolveByClassUnification ) );
                r.Services.SimpleMappings[typeof( ResolveByClassUnification )].Should().BeSameAs( typeof( ResolveByClassUnification ) );
                r.Services.SimpleMappings[typeof( ServiceImpl1 )].Should().BeSameAs( typeof( ServiceImpl1 ) );
            }
            else
            {
                var r = CheckFailure( collector );
                var interfaces = r.AmbientTypeResult.AmbientServices.LeafInterfaces;
                interfaces.Should().HaveCount( 1 );
                var classes = r.AmbientTypeResult.AmbientServices.RootClasses;
                classes.Select( c => c.Type ).Should().BeEquivalentTo( typeof( ServiceImplBaseBase ) );
                r.AmbientTypeResult.AmbientServices.ClassAmbiguities.Should().HaveCount( 1 );
                r.AmbientTypeResult.AmbientServices.ClassAmbiguities[0]
                    .Select( c => c.Type )
                    .Should().BeEquivalentTo( typeof( ServiceImplRootProblem ), typeof( ServiceImpl1 ), typeof( ServiceImpl3 ) );
            }
        }

        class S1 : ISBase
        {
            public S1( S2 s2 )
            {
            }
        }

        class S2 : ISBase
        {
            public S2( S3 s2 )
            {
            }
        }

        class S3 : ISBase
        {
            public S3( S4 s2 )
            {
            }
        }

        class S4 : ISBase
        {
        }

        [Test]
        public void simple_linked_list_of_service_classes()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( S1 ) );
            collector.RegisterType( typeof( S2 ) );
            collector.RegisterType( typeof( S3 ) );
            collector.RegisterType( typeof( S4 ) );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( ISBase )].Should().BeSameAs( typeof( S1 ) );
            r.Services.SimpleMappings[typeof( S1 )].Should().BeSameAs( typeof( S1 ) );
            r.Services.SimpleMappings[typeof( S2 )].Should().BeSameAs( typeof( S2 ) );
            r.Services.SimpleMappings[typeof( S3 )].Should().BeSameAs( typeof( S3 ) );
            r.Services.SimpleMappings[typeof( S4 )].Should().BeSameAs( typeof( S4 ) );
        }

        public abstract class AbstractS1 : ISBase
        {
            public AbstractS1( AbstractS2 s2 )
            {
            }
        }

        public abstract class AbstractS2 : ISBase
        {
            public AbstractS2( AbstractS3 s3 )
            {
            }
        }

        public abstract class AbstractS3 : ISBase
        {
            public AbstractS3()
            {
            }
        }

        [Test]
        public void Linked_list_of_service_abstract_classes()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( AbstractS1 ) );
            collector.RegisterType( typeof( AbstractS2 ) );
            collector.RegisterType( typeof( AbstractS3 ) );
            var (r, map) = CheckSuccessAndEmit( collector );

            var final = r.Services.SimpleMappings[typeof( ISBase )];
            final.Should().NotBeSameAs( typeof( AbstractS1 ) );
            final.Should().BeAssignableTo( typeof( AbstractS1 ) );
            r.Services.SimpleMappings[typeof( AbstractS1 )].Should().BeSameAs( final );

            r.Services.SimpleMappings[typeof( AbstractS2 )].Should().NotBeSameAs( typeof( AbstractS2 ) );
            r.Services.SimpleMappings[typeof( AbstractS2 )].Should().BeAssignableTo( typeof( AbstractS2 ) );
            r.Services.SimpleMappings[typeof( AbstractS3 )].Should().NotBeSameAs( typeof( AbstractS3 ) );
            r.Services.SimpleMappings[typeof( AbstractS3 )].Should().BeAssignableTo( typeof( AbstractS3 ) );

            IServiceProvider p = TestHelper.CreateAndConfigureSimpleContainer( map );
            var oG = p.GetService<ISBase>();
            oG.GetType().FullName.Should().StartWith( "CK._g.AbstractS1" );

        }


    }
}
