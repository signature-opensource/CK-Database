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
    public class UnificationTests : TestsBase
    {
        interface ISBase : IAmbientService
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
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClass( typeof( ServiceS1S2Impl ) );
                var r = CheckFailure( collector );
                r.AmbientServices.HasFatalError.Should().BeTrue( "The Service unification is required." );
                r.AmbientServices.Classes.Should().HaveCount( 1 );
            }
            {
                var collector = CreateAmbientTypeCollector();
                collector.RegisterClass( typeof( ServiceS1Impl ) );
                collector.RegisterClass( typeof( ServiceS2Impl ) );
                var r = CheckFailure( collector );
                r.AmbientServices.HasFatalError.Should().BeTrue( "The Service unification is required." );
                r.AmbientServices.Classes.Should().HaveCount( 2 );
            }
            // Same tests as above but excluding ISBase type: success since
            // ISBase is no more considered a IAmbientService.
            {
                var collector = CreateAmbientTypeCollector( t => t != typeof(ISBase) );
                collector.RegisterClass( typeof( ServiceS1S2Impl ) );
                var r = CheckSuccess( collector );
                r.AmbientServices.Classes.Should().HaveCount( 1 );
            }
            {
                var collector = CreateAmbientTypeCollector( t => t != typeof( ISBase ) );
                collector.RegisterClass( typeof( ServiceS1Impl ) );
                collector.RegisterClass( typeof( ServiceS2Impl ) );
                var r = CheckSuccess( collector );
                r.AmbientServices.Classes.Should().HaveCount( 2 );
            }
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
            var collector = CreateAmbientTypeCollector();
            collector.RegisterClass( typeof( ServiceUnifiedImpl ) );
            var r = CheckSuccess( collector );
            var interfaces = r.AmbientServices.Interfaces;
            interfaces.Should().HaveCount( 1 );
            var iSU = interfaces[0];
            iSU.InterfaceType.Should().Be( typeof( ISU ) );
            iSU.Interfaces.Select( i => i.InterfaceType ).Should().BeEquivalentTo( typeof(ISBase), typeof(IS1), typeof(IS2) );
            r.AmbientServices.Classes.Should().ContainSingle().And.Contain( c => c.Type == typeof( ServiceUnifiedImpl ) );
        }

        interface IMultiImplService : IAmbientService
        {
        }

        class ServiceImplBaseBase : IMultiImplService
        {
        }

        class ServiceImplRootProblem : ServiceImplBaseBase
        {
        }

        class ServiceImpl1 : ServiceImplRootProblem
        {
        }

        class ServiceImpl2 : ServiceImplRootProblem
        {
        }

        class ServiceImpl3 : ServiceImpl2
        {
        }

        [Test]
        public void service_classes_require_unification()
        {
            var collector = CreateAmbientTypeCollector();
            collector.RegisterClass( typeof( ServiceImpl1 ) );
            collector.RegisterClass( typeof( ServiceImpl3 ) );
            var r = CheckSuccess( collector );
            var interfaces = r.AmbientServices.Interfaces;
            interfaces.Should().HaveCount( 1 );
            var classes = r.AmbientServices.Classes;
            classes.Should().HaveCount( 2 );
            classes.Select( c => c.Type ).Should().BeEquivalentTo( typeof( ServiceImpl1 ), typeof( ServiceImpl3 ) );
            r.AmbientServices.UnificationRequired.Select( c => c.Type )
                .Should().BeEquivalentTo( typeof( ServiceImplRootProblem ) );
        }


    }
}
