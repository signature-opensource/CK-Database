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
            using( var context = new SqlSetupContext( "Server=.;Database=IntoTheWild;Integrated Security=SSPI;", TestHelper.Logger ) )
            {
                if( !context.DefaultSqlDatabase.IsOpen() ) context.DefaultSqlDatabase.OpenOrCreate( ".", "IntoTheWild" );
                context.AssemblyRegistererConfiguration.DiscoverAssemblyNames.Add( "IntoTheWild0" );

                SqlSetupCenter c = new SqlSetupCenter( context );
                Assert.That( c.Run() );
            }
        }
    }
}
