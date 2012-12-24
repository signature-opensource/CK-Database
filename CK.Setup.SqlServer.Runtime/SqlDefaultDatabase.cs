using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlDefaultDatabase : SqlDatabase, IAmbientContract
    {
        public void Construct( string connectionString = null )
        {
            ConnectionString = connectionString;
            EnsureSchema( DefaultSchemaName );
        }
    }
}
