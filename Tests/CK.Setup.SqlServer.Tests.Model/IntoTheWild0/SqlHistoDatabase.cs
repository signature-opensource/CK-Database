using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    [RemoveDefaultContext]
    [AddContext(typeof(SqlHistoDatabase))]
    public class SqlHistoDatabase : SqlDatabase, IAmbiantContract
    {
        public SqlHistoDatabase()
            : base( "dbHisto" )
        {
            InstallCore = true;
        }

    }
}
