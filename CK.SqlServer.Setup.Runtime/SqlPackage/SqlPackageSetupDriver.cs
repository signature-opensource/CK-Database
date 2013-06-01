using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Setup
{
    public class SqlPackageSetupDriver : SqlPackageBaseSetupDriver
    {
        public SqlPackageSetupDriver( BuildInfo info )
            : base( info ) 
        {
            string schema = Item.Object.Schema;
            if( schema != null && Item.Object.Database != null ) Item.Object.Database.EnsureSchema( schema ); 
        }

        public new SqlPackageItem Item { get { return (SqlPackageItem)base.Item; } }

    }
}
