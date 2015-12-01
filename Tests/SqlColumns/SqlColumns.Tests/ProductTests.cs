using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;

namespace SqlColumns.Tests
{
    [TestFixture]
    public class ProductTests
    {
        [Test]
        public void iterating_over_product_columns()
        {
            ProductTable  t = TestHelper.StObjMap.Default.Obtain<ProductTable>();
            //Assert.That( t.Columns.Count, Is.EqualTo( 2 ) );
        }
    }
}
