using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlDatabaseItem : PackageTypeItem, IDependentItemDiscoverer
    {

        public SqlDatabaseItem( Type itemType, PackageAttribute attr )
            : base( itemType, attr, "SqlDatabase" )
        {
            SqlConnection = new SqlConnectionItem( this );
        }

        public SqlConnectionItem SqlConnection { get; private set; }

        public SqlDatabase Database { get; private set; }

        
    }
}
