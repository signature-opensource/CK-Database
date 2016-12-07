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
    }

}
