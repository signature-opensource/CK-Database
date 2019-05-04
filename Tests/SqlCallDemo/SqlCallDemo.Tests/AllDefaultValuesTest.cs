using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class AllDefaultValuesTest
    {
        [Test]
        public void all_default_values_at_work()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<AllDefaultValuesPackage>();
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
                                + " - @Float = -4.57586e-006"
                                + " - @Real = -4.5588e-009"
                                + " - @Bin = 0A3B"
                                + " - @Char = c"
                              ) );
            }
        }

    }
}
