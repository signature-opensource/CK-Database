using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class CharAndNCharTest
    {
        [Test]
        public async Task async_call_to_functions_returns_the_char()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<CharAndNCharPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                char c1 = await p.CharFunctionAsync( ctx, 'a' ).ConfigureAwait( false );
                char c2 = await p.CharFunctionAsync( ctx, null ).ConfigureAwait( false );
                char cN1 = await p.NCharFunctionAsync( ctx, 'n' ).ConfigureAwait( false );
                char cN2 = await p.NCharFunctionAsync( ctx, null ).ConfigureAwait( false );
                c1.Should().Be( 'a' );
                c2.Should().Be( '~' );
                cN1.Should().Be( 'n' );
                cN2.Should().Be( '~' );
            }
        }

        [Test]
        public void call_to_sCharProc()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<CharAndNCharPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                p.CharProc( ctx, 'a', null, 'b', null, out var cO, out var cNO );
                cO.Should().Be( 'a' );
                cNO.Should().Be( 'b' );

                p.CharProc( ctx, 'a', 'A', 'b', 'B', out cO, out cNO );
                cO.Should().Be( 'A' );
                cNO.Should().Be( 'B' );
            }
        }

        [Test]
        public async Task async_call_to_sCharProc()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<CharAndNCharPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var r = await p.CharProcAsync( ctx, 'a', null, 'b', null );
                r.CO.Should().Be( 'a' );
                r.CNO.Should().Be( 'b' );

                r = await p.CharProcAsync( ctx, 'a', 'A', 'b', 'B' );
                r.CO.Should().Be( 'A' );
                r.CNO.Should().Be( 'B' );

                r = await p.CharProcAsync( ctx, 'X', null, 'Y', null );
                r.CO.Should().Be( 'X' );
                r.CNO.Should().Be( 'Y' );
            }
        }

    }
}
