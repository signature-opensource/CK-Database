using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using static CK.Testing.SqlServerTestHelper;

namespace SqlCallDemo.Tests;

[TestFixture]
public class CharAndNCharTest
{
    [Test]
    public async Task async_call_to_functions_returns_the_char_Async()
    {
        var p = SharedEngine.Map.StObjs.Obtain<CharAndNCharPackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            char c1 = await p.CharFunctionAsync( ctx, 'a' ).ConfigureAwait( false );
            char c2 = await p.CharFunctionAsync( ctx, null ).ConfigureAwait( false );
            char cN1 = await p.NCharFunctionAsync( ctx, 'n' ).ConfigureAwait( false );
            char cN2 = await p.NCharFunctionAsync( ctx, null ).ConfigureAwait( false );
            c1.ShouldBe( 'a' );
            c2.ShouldBe( '~' );
            cN1.ShouldBe( 'n' );
            cN2.ShouldBe( '~' );
        }
    }

    [Test]
    public void call_to_sCharProc()
    {
        var p = SharedEngine.Map.StObjs.Obtain<CharAndNCharPackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            p.CharProc( ctx, 'a', null, 'b', null, out var cO, out var cNO );
            cO.ShouldBe( 'a' );
            cNO.ShouldBe( 'b' );

            p.CharProc( ctx, 'a', 'A', 'b', 'B', out cO, out cNO );
            cO.ShouldBe( 'A' );
            cNO.ShouldBe( 'B' );
        }
    }

    [Test]
    public async Task async_call_to_sCharProc_Async()
    {
        var p = SharedEngine.Map.StObjs.Obtain<CharAndNCharPackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var r = await p.CharProcAsync( ctx, 'a', null, 'b', null );
            r.CO.ShouldBe( 'a' );
            r.CNO.ShouldBe( 'b' );

            r = await p.CharProcAsync( ctx, 'a', 'A', 'b', 'B' );
            r.CO.ShouldBe( 'A' );
            r.CNO.ShouldBe( 'B' );

            r = await p.CharProcAsync( ctx, 'X', null, 'Y', null );
            r.CO.ShouldBe( 'X' );
            r.CNO.ShouldBe( 'Y' );
        }
    }

}
