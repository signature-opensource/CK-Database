using FluentAssertions;
using NUnit.Framework;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static CK.Testing.DBSetupTestHelper;

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    public class SqlCommandAndConnectionExtensionTests
    {
        [Test]
        public async Task ExecuteScalarAsync_works_on_closed_or_opened_connection()
        {
            using( var cmd = new SqlCommand( "select count(*) from sys.tables" ) )
            using( var c = new SqlConnection( TestHelper.GetConnectionString() ) )
            {
                await c.OpenAsync();
                cmd.ExecuteScalar( c, -1 ).Should().BeGreaterThan( 0 );
                c.Close();
                (await cmd.ExecuteScalarAsync( c, -1 )).Should().BeGreaterThan( 0 );
                cmd.CommandText = "select count(*) from sys.tables where name='notablehere'";
                await c.OpenAsync();
                (await cmd.ExecuteScalarAsync( c, -1 )).Should().Be( 0 );
                c.Close();
                (await cmd.ExecuteScalarAsync( c, -1 )).Should().Be( 0 );
                cmd.CommandText = "select name from sys.tables where name='notablehere'";
                await c.OpenAsync();
                (await cmd.ExecuteScalarAsync( c, -1 )).Should().Be( -1 );
                c.Close();
                (await cmd.ExecuteScalarAsync( c, -1 )).Should().Be( -1 );
            }
        }

        [Test]
        public void ExecuteScalar_works_on_closed_or_opened_connection()
        {
            using( var cmd = new SqlCommand( "select count(*) from sys.tables" ) )
            using( var c = new SqlConnection( TestHelper.GetConnectionString() ) )
            {
                c.Open();
                cmd.ExecuteScalar( c, -1 ).Should().BeGreaterThan( 0 );
                c.Close();
                cmd.ExecuteScalar( c, -1 ).Should().BeGreaterThan( 0 );
                cmd.CommandText = "select count(*) from sys.tables where name='notablehere'";
                c.Open();
                cmd.ExecuteScalar( c, -1 ).Should().Be( 0 );
                c.Close();
                cmd.ExecuteScalar( c, -1 ).Should().Be( 0 );
                cmd.CommandText = "select name from sys.tables where name='notablehere'";
                c.Open();
                cmd.ExecuteScalar( c, -1 ).Should().Be( -1 );
                c.Close();
                cmd.ExecuteScalar( c, -1 ).Should().Be( -1 );
            }
        }
    }
}
