using CK.Core;
using CK.SqlServer;
using CK.Testing;
using NUnit.Framework;
using System;
using static CK.Testing.SqlServerTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class AllDefaultValuesTest
    {
        [Test]
        public void all_default_values_at_work()
        {
            var p = SharedEngine.Map.StObjs.Obtain<AllDefaultValuesPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                Assert.That( p.AllDefaultValues( ctx ), 
                    Is.EqualTo( "@NVarChar = All Defaults" 
                                + " - @Int = 3712"
                                + " - @BigInt = 9223372036854775807"
                                + " - @SmallInt = -32768"
                                + " - @TinyInt = 255 - @Bit = 1"
                                + " - @Numeric = 123456789012345678" 
                                + " - @Numeric2010 = 1234567890.0123456789"
                                + " - @DateTime = 2011-10-26T00:00:00"
                                + " - @DateTime2 = 2011-10-26T00:00:00"
                                + " - @Time = 12:34:56.78"
                                + " - @Float = -4.57586e-006"
                                + " - @Real = -4.5588e-009"
                                + " - @Bin = 0A3B"
                                + " - @Char = c"
                              ) );
            }
        }

        [Test]
        public void all_default_values_but_time_at_work()
        {
            var p = SharedEngine.Map.StObjs.Obtain<AllDefaultValuesPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                Assert.That( p.AllDefaultValuesButTime( ctx, new TimeSpan( 0, 1, 2, 3, 40 ) ), 
                    Is.EqualTo( "@NVarChar = All Defaults" 
                                + " - @Int = 3712"
                                + " - @BigInt = 9223372036854775807"
                                + " - @SmallInt = -32768"
                                + " - @TinyInt = 255 - @Bit = 1"
                                + " - @Numeric = 123456789012345678" 
                                + " - @Numeric2010 = 1234567890.0123456789"
                                + " - @DateTime = 2011-10-26T00:00:00"
                                + " - @DateTime2 = 2011-10-26T00:00:00"
                                + " - @Time = 01:02:03.04"
                                + " - @Float = -4.57586e-006"
                                + " - @Real = -4.5588e-009"
                                + " - @Bin = 0A3B"
                                + " - @Char = c"
                              ) );
            }
        }

    }
}
