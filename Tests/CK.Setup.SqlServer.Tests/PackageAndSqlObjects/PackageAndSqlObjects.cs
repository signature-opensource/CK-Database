using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Reflection;

namespace CK.Setup.SqlServer.Tests
{

    [TestFixture]
    public class PackageAndSqlObjects
    {
        [Test]
        public void IntoTheWild0()
        {
            string connection = "Server=.;Database=IntoTheWild;Integrated Security=SSPI;";
            using( var context = new SqlSetupContext( connection, TestHelper.Logger ) )
            {
                context.SqlDatabases.Add( "dbHisto", connection );
                context.AssemblyRegistererConfiguration.DiscoverAssemblyNames.Add( "IntoTheWild0" );

                // Try normally with any existing database if it exists.
                {
                    if( !context.DefaultSqlDatabase.IsOpen() ) context.DefaultSqlDatabase.OpenOrCreate( ".", "IntoTheWild" );
                    SqlSetupCenter c = new SqlSetupCenter( context );
                    Assert.That( c.Run() );
                    Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
                }
                // Drop CK and CKCore and retries in reverse order.
                {
                    context.DefaultSqlDatabase.SchemaDropAllObjects( "CK", true );
                    context.DefaultSqlDatabase.SchemaDropAllObjects( "CKCore", false );
                    Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tRes','tResDataRawText')" ), Is.EqualTo( 0 ) );

                    SqlSetupCenter c = new SqlSetupCenter( context );
                    c.RevertOrderingNames = true;
                    Assert.That( c.Run() );
                    Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
                }
            }
        }
    }
}
