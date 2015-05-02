using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    public class TestSqlCallContext : ISqlCallContext
    {
        public TestSqlCallContext()
        {
        }

        #region ISqlCallContext Members

        public SqlConnectionProvider GetProvider( string connectionString )
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
