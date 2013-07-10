using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Setup
{
    public class SqlDefaultDatabase : SqlDatabase, IAmbientContract
    {
        public SqlDefaultDatabase()
            : base( DefaultDatabaseName )
        {
            EnsureSchema( DefaultSchemaName );
        }

        void Construct( string connectionString )
        {
            ConnectionString = connectionString;
        }
    }
}
