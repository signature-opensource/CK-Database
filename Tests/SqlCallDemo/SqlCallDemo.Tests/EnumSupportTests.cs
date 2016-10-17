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

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class EnumSupportTests
    {
        [Test]
        public async Task async_call_with_enum_values()
        {
            var p = TestHelper.StObjMap.Default.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                FunctionPackage.Power t1 = await p.ProcWithEnumIOAsync( ctx, FunctionPackage.BPower.One, FunctionPackage.Power.Max );
                FunctionPackage.Power t2 = await p.ProcWithEnumIOAsync( ctx, FunctionPackage.BPower.Two, FunctionPackage.Power.Min );
                FunctionPackage.Power t3 = await p.ProcWithEnumIOAsync( ctx, FunctionPackage.BPower.Zero, FunctionPackage.Power.Med );
                Assert.That( t1, Is.EqualTo( FunctionPackage.Power.Overheat ) );
                Assert.That( t2, Is.EqualTo( FunctionPackage.Power.Max ) );
                Assert.That( t3, Is.EqualTo( FunctionPackage.Power.Med ) );
            }
        }

        [Test]
        public void call_with_enum_values()
        {
            var p = TestHelper.StObjMap.Default.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                FunctionPackage.Power t1 = p.ProcWithEnumIO( ctx, FunctionPackage.BPower.One, FunctionPackage.Power.Max );
                FunctionPackage.Power t2 = p.ProcWithEnumIO( ctx, FunctionPackage.BPower.Two, FunctionPackage.Power.Min );
                FunctionPackage.Power t3 = p.ProcWithEnumIO( ctx, FunctionPackage.BPower.Zero, FunctionPackage.Power.Med );
                Assert.That( t1, Is.EqualTo( FunctionPackage.Power.Overheat ) );
                Assert.That( t2, Is.EqualTo( FunctionPackage.Power.Max ) );
                Assert.That( t3, Is.EqualTo( FunctionPackage.Power.Med ) );
            }
        }

        [Test]
        public async Task async_call_with_nullable_enum_values()
        {
            var p = TestHelper.StObjMap.Default.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                FunctionPackage.Power? t1 = await p.ProcWithNullableEnumIOAsync( ctx, FunctionPackage.BPower.One, FunctionPackage.Power.Max );
                FunctionPackage.Power? t2 = await p.ProcWithNullableEnumIOAsync( ctx, null, FunctionPackage.Power.Min );
                FunctionPackage.Power? t3 = await p.ProcWithNullableEnumIOAsync( ctx, FunctionPackage.BPower.Zero, null );
                Assert.That( t1, Is.EqualTo( FunctionPackage.Power.Overheat ) );
                Assert.That( t2, Is.Null );
                Assert.That( t3, Is.Null );
            }
        }

        [Test]
        public void call_with_nullable_enum_values()
        {
            var p = TestHelper.StObjMap.Default.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                FunctionPackage.Power? t1 = p.ProcWithNullableEnumIO( ctx, FunctionPackage.BPower.One, FunctionPackage.Power.Max );
                FunctionPackage.Power? t2 = p.ProcWithNullableEnumIO( ctx, null, FunctionPackage.Power.Min );
                FunctionPackage.Power? t3 = p.ProcWithNullableEnumIO( ctx, FunctionPackage.BPower.Zero, null );
                Assert.That( t1, Is.EqualTo( FunctionPackage.Power.Overheat ) );
                Assert.That( t2, Is.Null );
                Assert.That( t3, Is.Null );
            }
        }

    }
}
