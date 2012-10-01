using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    [RemoveDefaultContext]
    [AddContext(typeof(SqlHistoDatabase))]
    public class SqlHistoDatabase : SqlDatabase, IAmbientContract
    {
        public void Construct( string connectionString = null )
        {
            ConnectionString = connectionString;
            Name = "dbHisto";
            InstallCore = true;
        }
        
    }
}
