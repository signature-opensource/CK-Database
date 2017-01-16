using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using CK.SqlServer.Setup;
using NUnit.Framework;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class PocoTests
    {
        [Test]
        public void calling_base_Write_actually_take_the_Poco_class()
        {
            var factoryThing = TestHelper.StObjMap.Default.Obtain<IPocoFactory<IThing>>();
            var factoryThingAH = TestHelper.StObjMap.Default.Obtain<IPocoFactory<PocoSupport.IThingWithAgeAndHeight>>();
            var p = TestHelper.StObjMap.Default.Obtain<PocoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var t = factoryThing.Create( o => o.Name = "a thing" );
                Assert.That( p.Write( ctx, t ), Is.EqualTo( "a thing P=0 A,H=0,0" ) );
                var tAH = factoryThingAH.Create( o =>
                {
                    o.Name = "aged thing";
                    o.Age = 12;
                    o.Height = 170;
                } );
                Assert.That( p.Write( ctx, tAH ), Is.EqualTo( "aged thing P=0 A,H=12,170" ) );
            }
        }

    }
}
