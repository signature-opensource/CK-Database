using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class EnumSupportTests
    {
        [Test]
        public async Task async_call_with_enum_values()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                FunctionPackage.Power t1 = await p.ProcWithEnumIOAsync( ctx, FunctionPackage.BPower.One, FunctionPackage.Power.Max ).ConfigureAwait( false );
                FunctionPackage.Power t2 = await p.ProcWithEnumIOAsync( ctx, FunctionPackage.BPower.Two, FunctionPackage.Power.Min ).ConfigureAwait( false );
                FunctionPackage.Power t3 = await p.ProcWithEnumIOAsync( ctx, FunctionPackage.BPower.Zero, FunctionPackage.Power.Med ).ConfigureAwait( false );
                Assert.That( t1, Is.EqualTo( FunctionPackage.Power.Overheat ) );
                Assert.That( t2, Is.EqualTo( FunctionPackage.Power.Max ) );
                Assert.That( t3, Is.EqualTo( FunctionPackage.Power.Med ) );
            }
        }

        [Test]
        public void call_with_enum_values()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<FunctionPackage>();
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
#if !NET461
        /// This capacity is NOT supported in IL.
        [Test]
        public void call_with_nullable_enum_values_by_ref()
        {
            FunctionPackage.Power? io;
            var p = TestHelper.StObjMap.StObjs.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                io = FunctionPackage.Power.Max;
                p.ProcWithNullableEnumIOByRef( ctx, FunctionPackage.BPower.One, ref io );
                Assert.That( io, Is.EqualTo( FunctionPackage.Power.Overheat ) );

                io = FunctionPackage.Power.Min;
                p.ProcWithNullableEnumIOByRef( ctx, FunctionPackage.BPower.Two, ref io );
                Assert.That( io, Is.EqualTo( FunctionPackage.Power.Max ) );

                io = FunctionPackage.Power.Med;
                p.ProcWithNullableEnumIOByRef( ctx, FunctionPackage.BPower.Zero, ref io );
                Assert.That( io, Is.EqualTo( FunctionPackage.Power.Med ) );

                io = null;
                p.ProcWithNullableEnumIOByRef( ctx, FunctionPackage.BPower.Zero, ref io );
                Assert.That( io, Is.Null );
            }
        }
#endif

        [Test]
        public async Task async_call_with_nullable_enum_values()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                FunctionPackage.Power? t1 = await p.ProcWithNullableEnumIOAsync( ctx, FunctionPackage.BPower.One, FunctionPackage.Power.Max ).ConfigureAwait( false );
                FunctionPackage.Power? t2 = await p.ProcWithNullableEnumIOAsync( ctx, null, FunctionPackage.Power.Min ).ConfigureAwait( false );
                FunctionPackage.Power? t3 = await p.ProcWithNullableEnumIOAsync( ctx, FunctionPackage.BPower.Zero, null ).ConfigureAwait( false );
                Assert.That( t1, Is.EqualTo( FunctionPackage.Power.Overheat ) );
                Assert.That( t2, Is.Null );
                Assert.That( t3, Is.Null );
            }
        }

        [Test]
        public void call_with_nullable_enum_values()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<FunctionPackage>();
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

        public static SqlCommand Cmd28()
        {
            var cmd = new SqlCommand( @"CK.sWithEnumIO" );
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter( @"@BytePower", SqlDbType.TinyInt );
            cmd.Parameters.Add( p1 );
            var p2 = new SqlParameter( @"@Power", SqlDbType.Int );
            p2.Direction = ParameterDirection.InputOutput;
            cmd.Parameters.Add( p2 );
            return cmd;
        }
    }
}
