using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlPackageType : IAmbiantContractDefiner
    {
        protected SqlPackageType()
        {
        }

        [AmbiantProperty]
        public SqlDatabase Database { get; set; }

        [AmbiantProperty]
        public string Schema { get; protected set; }

    }
}
