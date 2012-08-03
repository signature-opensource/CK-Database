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
        {
            Name = DefaultDatabaseName;
            EnsureSchema( "CK" );
            EnsureSchema( "CKCore" );
        }
        
        public override bool IsDefaultDatabase
        {
            get { return true; }
        }

    }
}
