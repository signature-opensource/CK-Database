using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.StObj.Engine.Tests.Service.StObj;
using CK.Setup;

namespace CK.StObj.Engine.Tests.Service.StObj
{
    [TestFixture]
    public class ManualMappingTests : TestsBase
    {
        static readonly Type[] SamplePackages =
        {
            typeof(Samples.RootPackage),
            typeof(Samples.P0),
            typeof(Samples.P1)
        };

        [Test]
        public void first_simple_manual_mapping()
        {
            Samples.ObjectNumber = 0;

            var collector = CreateStObjCollector( SamplePackages );
            collector.RegisterType( typeof( Samples.SFront1 ) );
            collector.RegisterType( typeof( Samples.SBaseLeaf ) );
            var (r, map) = CheckSuccessAndEmit( collector );

            r.Services.SimpleMappings[typeof( Samples.ISBase )].Should().BeNull();

            IStObjServiceClassFactory f = r.Services.ManualMappings[typeof( Samples.ISBase )];
            f.Should().NotBeNull();

            var serviceProvider = new SimpleServiceContainer();
            var o = f.CreateInstance( serviceProvider );
            o.Should().NotBeNull().And.BeOfType<Samples.SFront1>();
            ((Samples.SFront1)o).Collect().Should().Be( "SFront1_2[SBaseLeaf_1]" );

            var oG = (Samples.ISBase)map.Services.ManualMappings[typeof( Samples.ISBase )].CreateInstance( serviceProvider );
            oG.Collect().Should().Be( "SFront1_4[SBaseLeaf_3]" );
        }

        [Test]
        public void SOnFront1_is_bound_to_SFront1()
        {
            var collector = CreateStObjCollector( SamplePackages );
            collector.RegisterType( typeof( Samples.SFront1 ) );
            collector.RegisterType( typeof( Samples.SBaseLeaf ) );
            collector.RegisterType( typeof( Samples.SOnFront1 ) );
            var (r, map) = CheckSuccessAndEmit( collector );

            r.Services.SimpleMappings[typeof( Samples.ISBase )].Should().BeNull();
            r.Services.ManualMappings[typeof( Samples.ISBase )].Should().NotBeNull();

            var info = map.Services.ManualMappings[typeof( Samples.ISBase )];
            info.ClassType.Should().BeSameAs( typeof( Samples.SOnFront1 ) );
            info.Assignments.Should().HaveCount( 1 );
            info.Assignments[0].ParameterType.Should().BeSameAs( typeof( Samples.SFront1 ) );
            var info2 = info.Assignments[0].Value;
            info2.ClassType.Should().BeSameAs( typeof( Samples.SFront1 ) );
            info2.Assignments.Should().HaveCount( 1 );
            info2.Assignments[0].ParameterType.Should().BeSameAs( typeof( Samples.ISBase ) );
            var info3 = info2.Assignments[0].Value;
            info3.ClassType.Should().BeSameAs( typeof( Samples.SBaseLeaf ) );
            info3.Assignments.Should().BeEmpty();

            Samples.ObjectNumber = 0;
            IServiceProvider p = TestHelper.CreateAndConfigureSimpleContainer( map );
            var oG = p.GetService<Samples.ISBase>();
            oG.Collect().Should().Be( "SOnFront1_3[SFront1_2[SBaseLeaf_1]]" );

            Samples.ObjectNumber = 100;
            IServiceProvider p100 = TestHelper.CreateAndConfigureSimpleContainer( map );
            var oG100 = p100.GetService<Samples.ISBase>();
            oG100.Collect().Should().Be( "SOnFront1_103[SFront1_102[SBaseLeaf_101]]" );

            var oG2 = p.GetService<Samples.ISBase>();
            oG2.Collect().Should().Be( "SOnFront1_3[SFront1_2[SBaseLeaf_1]]" );
        }

        (StObjCollectorResult, IStObjMap) GetSFront1InP1Graph()
        {
            var collector = CreateStObjCollector( SamplePackages );
            collector.RegisterType( typeof( Samples.SFront1 ) );
            collector.RegisterType( typeof( Samples.SBaseLeaf ) );
            collector.RegisterType( typeof( Samples.SOnFront1 ) );
            collector.RegisterType( typeof( Samples.SFront1InP1 ) );
            return CheckSuccessAndEmit( collector );
        }

        [Test]
        public void SFront1InP1_is_necessarily_before()
        {
            var (r, map) = GetSFront1InP1Graph();
            Samples.ObjectNumber = 0;
            IServiceProvider p = TestHelper.CreateAndConfigureSimpleContainer( map );
            var o = p.GetService<Samples.ISBase>();
            o.Collect().Should().Be( "SFront1InP1_4[SOnFront1_3[SFront1_2[SBaseLeaf_1]]]" );
        }


        [Test]
        public void asking_for_a_link_class_bound_to_the_abstraction_in_a_chain_leads_to_funny_result()
        {
            var (r, map) = GetSFront1InP1Graph();
            Samples.ObjectNumber = 0;
            IServiceProvider p = TestHelper.CreateAndConfigureSimpleContainer( map );

            Samples.ObjectNumber = 0;
            // SFront1( ISBase next )... So it is linked to another instance of itself...
            var o = p.GetService<Samples.SFront1>();
            o.Collect().Should().Be( "SFront1_5[SFront1InP1_4[SOnFront1_3[SFront1_2[SBaseLeaf_1]]]]" );
        }

    }
}
