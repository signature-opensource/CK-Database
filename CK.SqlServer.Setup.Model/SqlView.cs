using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public abstract class SqlView : SqlPackageBase, IAmbientContractDefiner
    {
        public SqlView()
        {
        }

        public SqlView( string viewName )
        {
        }

        public string ViewName { get; set; }

        public string SchemaName { get { return Schema + '.' + ViewName; } }

    }

}
