using CK.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    public class SqlCommandAndConnectionExtensionTests
    {
        [Test]
        public async Task ExecuteScalarAsync_works_on_closed_or_opened_connection()
        {
            using (var cmd = new SqlCommand("select count(*) from sys.tables"))
            using (var c = new SqlConnection(TestHelper.DatabaseTestConnectionString))
            {
                await c.OpenAsync();
                Assert.That(cmd.ExecuteScalar(c, -1), Is.GreaterThan(0));
                c.Close();
                Assert.That( await cmd.ExecuteScalarAsync(c, -1), Is.GreaterThan(0));
                cmd.CommandText = "select count(*) from sys.tables where name='notablehere'";
                await c.OpenAsync();
                Assert.That( await cmd.ExecuteScalarAsync(c, -1), Is.EqualTo(0));
                c.Close();
                Assert.That(await cmd.ExecuteScalarAsync(c, -1), Is.EqualTo(0));
                cmd.CommandText = "select name from sys.tables where name='notablehere'";
                await c.OpenAsync();
                Assert.That(await cmd.ExecuteScalarAsync(c, -1), Is.EqualTo(-1));
                c.Close();
                Assert.That(await cmd.ExecuteScalarAsync(c, -1), Is.EqualTo(-1));
            }
        }

        [Test]
        public void ExecuteScalar_works_on_closed_or_opened_connection()
        {
            using (var cmd = new SqlCommand("select count(*) from sys.tables"))
            using (var c = new SqlConnection(TestHelper.DatabaseTestConnectionString))
            {
                c.Open();
                Assert.That(cmd.ExecuteScalar(c, -1), Is.GreaterThan(0));
                c.Close();
                Assert.That(cmd.ExecuteScalar(c, -1), Is.GreaterThan(0));
                cmd.CommandText = "select count(*) from sys.tables where name='notablehere'";
                c.Open();
                Assert.That(cmd.ExecuteScalar(c, -1), Is.EqualTo(0));
                c.Close();
                Assert.That(cmd.ExecuteScalar(c, -1), Is.EqualTo(0));
                cmd.CommandText = "select name from sys.tables where name='notablehere'";
                c.Open();
                Assert.That(cmd.ExecuteScalar(c, -1), Is.EqualTo(-1));
                c.Close();
                Assert.That(cmd.ExecuteScalar(c, -1), Is.EqualTo(-1));
            }
        }
    }
}
