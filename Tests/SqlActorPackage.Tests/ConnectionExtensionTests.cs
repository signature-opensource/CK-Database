using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using SqlActorPackage.Basic;
using System.Diagnostics;
using NUnit.Framework.Constraints;
using System.Data.SqlClient;
using CK.SqlServer;
using System.Data;

namespace SqlActorPackage.Tests
{
    [TestFixture]
    public class ConnectionExtensionTests
    {
        [Test]
        public void using_extension_methods_synchronous()
        {
            var a = TestHelper.StObjMap.Default.Obtain<ActorHome>();
            var c = new SqlConnection( a.Database.ConnectionString );
            string tableName = "CK.t" + Guid.NewGuid().ToString( "N" );
            var create = new SqlCommand( $"create table {tableName} ( id int, name varchar(10) ); insert into {tableName}(id,name) values (1,'One'), (2,'Two'), (3,'Three');" );
            var scalar = new SqlCommand( $"select name from {tableName} where id=@Id;" );
            scalar.Parameters.AddWithValue( "@Id", 3 );
            var row = new SqlCommand( $"select top 1 id, name from {tableName} order by id;" );
            var reader = new SqlCommand( $"select id, name from {tableName} order by id;" );
            try
            {
                Assert.That( c.State, Is.EqualTo( ConnectionState.Closed ) );
                using( var disposer = c.EnsureOpen() )
                {
                    Assert.That( disposer, Is.Not.Null );
                    Assert.That( c.State, Is.EqualTo( ConnectionState.Open ) );
                    Assert.That( c.EnsureOpen(), Is.Null );
                    Assert.DoesNotThrow( () => create.ExecuteNonQuery( c ) );
                    Assert.That( scalar.ExecuteScalar<string>( c ), Is.EqualTo( "Three" ) );
                    var rowResult = row.ExecuteRow( c, r => Tuple.Create( r.GetInt32( 0 ), r.GetString( 1 ) ) );
                    Assert.That( rowResult.Item1, Is.EqualTo( 1 ) );
                    Assert.That( rowResult.Item2, Is.EqualTo( "One" ) );
                    var readerResult = reader.ExecuteReader<Tuple<int, string>>( c, ( r, list ) => list.Add( Tuple.Create( r.GetInt32( 0 ), r.GetString( 1 ) ) ) );
                    Assert.That( readerResult.Count, Is.EqualTo( 3 ) );
                    Assert.That( readerResult[0].Item1, Is.EqualTo( 1 ) );
                    Assert.That( readerResult[1].Item2, Is.EqualTo( "Two" ) );
                    Assert.That( readerResult[2].Item2, Is.EqualTo( "Three" ) );
                }
            }
            finally
            {
                using( var closing = new SqlConnection( a.Database.ConnectionString ) )
                {
                    closing.Open();
                    var clean = new SqlCommand( $"drop table {tableName};" );
                    clean.ExecuteNonQuery( closing );
                }
            }
        }


        [Test]
        public async Task using_extension_methods_async()
        {
            var a = TestHelper.StObjMap.Default.Obtain<ActorHome>();
            var c = new SqlConnection( a.Database.ConnectionString );
            string tableName = "CK.t" + Guid.NewGuid().ToString( "N" );
            var create = new SqlCommand( $"create table {tableName} ( id int, name varchar(10) ); insert into {tableName}(id,name) values (1,'One'), (2,'Two'), (3,'Three');" );
            var scalar = new SqlCommand( $"select name from {tableName} where id=@Id;" );
            scalar.Parameters.AddWithValue( "@Id", 3 );
            var row = new SqlCommand( $"select top 1 id, name from {tableName} order by id;" );
            var reader = new SqlCommand( $"select id, name from {tableName} order by id;" );
            try
            {
                Assert.That( c.State, Is.EqualTo( ConnectionState.Closed ) );
                using( var disposer = await c.EnsureOpenAsync() )
                {
                    Assert.That( disposer, Is.Not.Null );
                    Assert.That( c.State, Is.EqualTo( ConnectionState.Open ) );
                    Assert.That( await c.EnsureOpenAsync(), Is.Null );
                    await create.ExecuteNonQueryAsync( c );
                    Assert.That( await scalar.ExecuteScalarAsync<string>( c ), Is.EqualTo( "Three" ) );
                    var rowResult = await row.ExecuteRowAsync( c, r => Tuple.Create( r.GetInt32( 0 ), r.GetString( 1 ) ) );
                    Assert.That( rowResult.Item1, Is.EqualTo( 1 ) );
                    Assert.That( rowResult.Item2, Is.EqualTo( "One" ) );
                    var readerResult = await reader.ExecuteReaderAsync<Tuple<int, string>>( c, ( r, list ) => list.Add( Tuple.Create( r.GetInt32( 0 ), r.GetString( 1 ) ) ) );
                    Assert.That( readerResult.Count, Is.EqualTo( 3 ) );
                    Assert.That( readerResult[0].Item1, Is.EqualTo( 1 ) );
                    Assert.That( readerResult[1].Item2, Is.EqualTo( "Two" ) );
                    Assert.That( readerResult[2].Item2, Is.EqualTo( "Three" ) );
                }
            }
            finally
            {
                using( var closing = new SqlConnection( a.Database.ConnectionString ) )
                {
                    await closing.OpenAsync();
                    var clean = new SqlCommand( $"drop table {tableName};" );
                    await clean.ExecuteNonQueryAsync( closing );
                }
            }
        }

        [Test]
        public void using_extension_methods_thows_a_SqlDetailedException()
        {
            var c = new SqlConnection( TestHelper.MasterConnectionString );
            Assert.Throws<SqlDetailedException>( () => new SqlCommand( "bug" ).ExecuteNonQuery( c ) );
            Assert.Throws<SqlDetailedException>( () => new SqlCommand( "bug" ).ExecuteScalar<int>( c ) );
            Assert.Throws<SqlDetailedException>( () => new SqlCommand( "bug" ).ExecuteRow<int>( c, r => 0 ) );
            Assert.Throws<SqlDetailedException>( () => new SqlCommand( "bug" ).ExecuteReader<int>( c, ( r, list ) => list.Add( 0 ) ) );
        }

        [Test]
        public async Task using_extension_methods_async_thows_a_SqlDetailedException()
        {
            await AssertThrows( c => new SqlCommand( "bug" ).ExecuteNonQueryAsync( c ) );
            await AssertThrows( c => new SqlCommand( "bug" ).ExecuteScalarAsync<int>( c ) );
            await AssertThrows( c => new SqlCommand( "bug" ).ExecuteRowAsync<int>( c, r => 0 ) );
            await AssertThrows( c => new SqlCommand( "bug" ).ExecuteReaderAsync<int>( c, (r,list) => list.Add(0) ) );
        }

        async Task AssertThrows( Func<SqlConnection,Task> run )
        {
            var c = new SqlConnection( TestHelper.MasterConnectionString );
            try
            {
                await run( c );
                Assert.Fail( "SqlDetailedException expected." );
            }
            catch( SqlDetailedException ex )
            {
                Assert.That( ex.Message, Does.Contain( "bug" ) );
            }
            Assert.That( c.State, Is.EqualTo( ConnectionState.Closed ) );
        }


        [Test]
        public void reading_big_text_with_execute_scalar_fails()
        {
            var con = new SqlConnection( TestHelper.MasterConnectionString );
            string read;

            SqlCommand cFailXml = new SqlCommand( "select * from sys.objects for xml path" );
            read = cFailXml.ExecuteScalar<string>( con );
            Assert.That( read.Length, Is.EqualTo( 2033 ), "2033 is the upper limit for ExecuteScalar." );

            SqlCommand cFailJson = new SqlCommand( "select * from sys.objects for json auto" );
            read = cFailJson.ExecuteScalar<string>( con );
            Assert.That( read.Length, Is.EqualTo( 2033 ), "2033 is the upper limit for ExecuteScalar." );

            // Using convert works for Json and Xml.
            SqlCommand cConvJson = new SqlCommand( "select convert( nvarchar(max), (select * from sys.objects for json auto))" );
            string readJsonConvert = cConvJson.ExecuteScalar<string>( con );
            Assert.That( readJsonConvert.Length, Is.GreaterThan( 20 * 1024 ) );

            SqlCommand cConvXml = new SqlCommand( "select convert( nvarchar(max), (select * from sys.objects for xml path))" );
            string readXmlConvert = cConvXml.ExecuteScalar<string>( con );
            Assert.That( readXmlConvert.Length, Is.GreaterThan( 20 * 1024 ) );

            // Using the SqlDataReader works for Json and Xml.
            SqlCommand cReaderJson = new SqlCommand( "select 1, Json = (select * from sys.objects for json auto)" );
            string readJsonViaReader = cReaderJson.ExecuteRow( con, r => r.GetString( 1 ) );
            Assert.That( readJsonViaReader.Length, Is.GreaterThan( 20 * 1024 ) );

            Assert.That( readJsonViaReader, Is.EqualTo( readJsonConvert ) );

            SqlCommand cReaderXml = new SqlCommand( "select Xml = (select * from sys.objects for xml path)" );
            string readXmlViaReader = cReaderXml.ExecuteRow( con, r => r.GetString( 0 ) );
            Assert.That( readXmlViaReader.Length, Is.GreaterThan( 20 * 1024 ) );

            Assert.That( readXmlViaReader, Is.EqualTo( readXmlConvert ) );
        }

    }

}
