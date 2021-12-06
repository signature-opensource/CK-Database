using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using SqlCallDemo.CommandDemo;
using System;
using System.Threading.Tasks;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class CommandDemoTests
    {
        [Test]
        public async Task command_pattern_just_work_with_poco_result_Async()
        {
            var cmd = new CmdDemo()
            {
                ActorId = 878,
                CompanyName = "Invenietis",
                LaunchDate = new DateTime( 2016, 6, 30 )
            };

            var p = TestHelper.StObjMap.StObjs.Obtain<CmdDemoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                CmdDemo.ResultPOCO r = await p.RunCommandAsync( ctx, cmd ).ConfigureAwait( false );
                Assert.That( r.Delay, Is.LessThan( 0 ) );
                Assert.That( r.ActualCompanyName, Is.EqualTo( "INVENIETIS HOP!" ) );
            }
        }

        [Test]
        public async Task command_pattern_just_work_with_a_clean_reaonly_poco_Async()
        {
            var cmd = new CmdDemo()
            {
                ActorId = 878,
                CompanyName = "Invenietis",
                LaunchDate = new DateTime( 2016, 6, 30 )
            };

            var p = TestHelper.StObjMap.StObjs.Obtain<CmdDemoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                CmdDemo.ResultReadOnly r = await p.RunCommandROAsync( ctx, cmd ).ConfigureAwait( false );
                Assert.That( r.Delay, Is.LessThan( 0 ) );
                Assert.That( r.ActualCompanyName, Is.EqualTo( "INVENIETIS HOP!" ) );
            }
        }


        [Test]
        public void calling_with_a_data_object()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<CmdDemoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                int id = p.CreateProtoUser( ctx, 67893, new ProtoUserData() { UserName = "jj", Email = "@", Phone = "06" } );
                Assert.That( id, Is.EqualTo( 67893 + 2 ) );
            }
        }

        [Test]
        public void when_parameter_source_name_conflicts()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<CmdDemoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                int id = p.ParameterSourceNamedTheSameAsOneOfTheActualParameters( ctx, 3712, new ProtoUserData() { UserName = "01234567", Email = "@", Phone = "06" } );
                Assert.That( id, Is.EqualTo( 3712 + 8 ) );
            }
        }

    }
}
