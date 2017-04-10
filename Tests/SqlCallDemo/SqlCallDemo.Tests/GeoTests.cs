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
using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class GeoTests
    {
        [Test]
        public void calling_a_function_with_a_SqlGeometry()
        {
            var geo = TestHelper.StObjMap.Default.Obtain<GeoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var g = SqlGeography.Parse(new SqlString("POINT(-77.010996 38.890358)"));
                double area = geo.Area(ctx, g);
                Assert.That(g.STArea(), Is.EqualTo(area));
            }
        }

    }
}