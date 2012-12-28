using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlTableSetupDriver : SqlPackageBaseSetupDriver
    {
        public SqlTableSetupDriver( BuildInfo info )
            : base( info ) 
        {
        }

        public new SqlTableItem Item { get { return (SqlTableItem)base.Item; } }

    }
}
