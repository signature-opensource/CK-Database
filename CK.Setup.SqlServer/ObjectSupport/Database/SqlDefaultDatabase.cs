using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlDefaultDatabase : SqlDatabase, IAmbiantContract
    {
        public SqlDefaultDatabase()
            : base()
        {
            EnsureSchema( "CK" );
        }
    }
}
