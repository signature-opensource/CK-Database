using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using CK.SqlServer.Setup;
using NUnit.Framework;
using SqlCallDemo.CommandDemo;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class CommandDemoTests
    {
        [Test]
        public async Task command_pattern_just_work_with_poco_result()
        {
            var cmd = new CmdDemo()
            {
                ActorId = 878,
                CompanyName = "Invenietis",
                LaunchnDate = new DateTime( 2016, 6, 30 )
            };

            var p = TestHelper.StObjMap.Default.Obtain<CmdDemoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                CmdDemo.ResultPOCO r = await p.RunCommandAsync( ctx, cmd );
                Assert.That( r.Delay, Is.LessThan( 0 ) );
                Assert.That( r.ActualCompanyName, Is.EqualTo( "INVENIETIS HOP!" ) );
            }
        }

        [Test]
        public async Task command_pattern_just_work_with_a_clean_reaonly_poco()
        {
            var cmd = new CmdDemo()
            {
                ActorId = 878,
                CompanyName = "Invenietis",
                LaunchnDate = new DateTime( 2016, 6, 30 )
            };

            var p = TestHelper.StObjMap.Default.Obtain<CmdDemoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                CmdDemo.ResultReadOnly r = await p.RunCommandROAsync( ctx, cmd );
                Assert.That( r.Delay, Is.LessThan( 0 ) );
                Assert.That( r.ActualCompanyName, Is.EqualTo( "INVENIETIS HOP!" ) );
            }
        }

    }
}
