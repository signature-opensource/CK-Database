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
    public class BasicTests : TestsBase
    {
        interface IServiceRegistered : IAmbientService
        {
        }

        interface IServiceNotRegisteredSinceNotImplemented : IServiceRegistered
        {
        }

        class ServiceRegisteredImpl : IServiceRegistered
        {
        }

        class ServiceNotRegisteredImpl : ServiceRegisteredImpl, IServiceNotRegisteredSinceNotImplemented
        {
        }

        [TestCase( "ExcludingSpecializedType" )]
        [TestCase( "NotRegisteringSpecializedType" )]
        public void service_interface_without_at_least_one_impl_are_ignored( string mode )
        {
            var collector = mode == "ExcludingSpecializedType"
                            ? CreateAmbientTypeCollector(  t => t != typeof( ServiceNotRegisteredImpl ) )
                            : CreateAmbientTypeCollector();

            collector.RegisterClassOrPoco( typeof( ServiceRegisteredImpl ) );
            if( mode == "ExcludingSpecializedType" )
            {
                collector.RegisterClassOrPoco( typeof( ServiceNotRegisteredImpl ) );
            }
            
            var r = CheckSuccess( collector );
            var interfaces = r.AmbientServices.LeafInterfaces;
            interfaces.Should().HaveCount( 1 );
            interfaces[0].Type.Should().Be( typeof( IServiceRegistered ) );
            var classes = r.AmbientServices.RootClasses;
            classes.Should().HaveCount( 1 );
            classes[0].IsExcluded.Should().BeFalse();
            classes[0].Generalization.Should().BeNull();
            classes[0].IsSpecialized.Should().BeFalse();
            classes[0].Type.Should().Be( typeof( ServiceRegisteredImpl ) );
            classes[0].Interfaces.Should().BeEquivalentTo( interfaces );
        }

        [Test]
        public void registering_service_registers_specialized_interfaces_and_base_impl_but_mask_them()
        {
            var collector = CreateAmbientTypeCollector();
            collector.RegisterClassOrPoco( typeof( ServiceNotRegisteredImpl ) );
            var r = CheckSuccess( collector );
            var interfaces = r.AmbientServices.LeafInterfaces;
            interfaces.Should().HaveCount( 1 );
            var iSpec = interfaces[0];
            var iBase = iSpec.Interfaces[0];
            iBase.Type.Should().Be( typeof( IServiceRegistered ) );
            iBase.SpecializationDepth.Should().Be( 0 );
            iBase.IsSpecialized.Should().BeTrue();
            iBase.Interfaces.Should().BeEmpty();
            iSpec.Type.Should().Be( typeof( IServiceNotRegisteredSinceNotImplemented ) );
            iSpec.SpecializationDepth.Should().Be( 1 );
            iSpec.IsSpecialized.Should().BeFalse();
            iSpec.Interfaces.Should().ContainSingle().And.Contain( iBase );
            var classes = r.AmbientServices.RootClasses;
            classes.Should().HaveCount( 1 );
            var cBase = classes[0];
            cBase.Type.Should().Be( typeof( ServiceRegisteredImpl ) );
            cBase.IsSpecialized.Should().BeTrue();
            var cSpec = cBase.MostSpecialized;
            cBase.Specializations.Should().ContainSingle().And.Contain( cSpec );
            cSpec.Type.Should().Be( typeof( ServiceNotRegisteredImpl ) );
            cSpec.Generalization.Should().BeSameAs( cBase );
            cSpec.Interfaces.Should().BeEquivalentTo( new[] { iBase, iSpec } );
        }

    }
}
