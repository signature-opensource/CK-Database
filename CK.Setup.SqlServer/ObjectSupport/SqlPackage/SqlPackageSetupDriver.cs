using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlPackageSetupDriver : SqlPackageBaseSetupDriver
    {
        public SqlPackageSetupDriver( BuildInfo info )
            : base( info ) 
        {
        }

        public new SqlPackageItem Item { get { return (SqlPackageItem)base.Item; } }

    }
}
