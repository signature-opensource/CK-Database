using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Reflection;
using CK.SqlServer;

namespace CK.Setup.SqlServer.Tests
{

    [TestFixture]
    public class PackageAndSqlObjects
    {
        [Test]
        public void IntoTheWild0()
        {
            using( var context = new SqlSetupContext( SqlManager.OpenOrCreate( ".", "IntoTheWild", TestHelper.Logger ) ) )
            {
                context.SqlDatabases.Add( "dbHisto", context.DefaultSqlDatabase.CurrentConnectionString );
                context.AssemblyRegistererConfiguration.DiscoverAssemblyNames.Add( "IntoTheWild0" );

                // Try normally with any existing database if it exists.
                {

                    SqlSetupCenter c = SqlSetupCenterFactory.Create( context );
                    Assert.That( c.Run() );
                    Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
                }
                // Drop CK and CKCore and retries in reverse order.
                {
                    context.DefaultSqlDatabase.SchemaDropAllObjects( "CK", true );
                    context.DefaultSqlDatabase.SchemaDropAllObjects( "CKCore", false );
                    Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tRes','tResDataRawText')" ), Is.EqualTo( 0 ) );

                    SqlSetupCenter c = SqlSetupCenterFactory.Create( context );
                    c.RevertOrderingNames = true;
                    Assert.That( c.Run() );
                    Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
                }
            }
        }
    }
}
