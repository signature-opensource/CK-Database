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
        public void InstallFromScratch()
        {
            using( var context = new SqlSetupContext( "Server=.;Database=PackageAndSqlObjects5;Integrated Security=SSPI;", TestHelper.Logger ) )
            {
                if( !context.DefaultDatabase.IsOpen() ) context.DefaultDatabase.OpenOrCreate( ".", "PackageAndSqlObjects5" );
                SqlSetupCenter c = new SqlSetupCenter( context );
                c.DiscoverObjects( Assembly.GetExecutingAssembly() );
                Assert.That( c.Run() );
            }
        }
    }
}
