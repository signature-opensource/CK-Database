using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using CK.SqlServer.Setup;

namespace CK.DB.Tests
{
    [TestFixture]
    public class DBSetup
    {
        [Test]
        [Explicit]
        public void reset_database_by_clearing_all_used_schemas()
        {
            TestHelper.ClearDatabaseUsedSchemas();
        }

        [Test]
        [Explicit]
        public void db_setup()
        {
            Assert.That( TestHelper.RunDBSetup(), "DBSetup failed." );
        }

        [Test]
        [Explicit]
        public void db_setup_with_StObj_and_Setup_graph_ordering_trace()
        {
            Assert.That( TestHelper.RunDBSetup( true, true ), "DBSetup failed." );
        }

        [Test]
        [Explicit]
        public void reverse_db_setup_with_StObj_and_Setup_graph_ordering_trace()
        {
            Assert.That( TestHelper.RunDBSetup( true, true, true ), "DBSetup failed." );
        }
    }
}
