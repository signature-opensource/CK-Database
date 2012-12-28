using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Setup
{
    public class SqlTable : SqlPackageBase, IAmbientContractDefiner
    {
        public SqlTable()
        {
        }

        public SqlTable( string tableName )
        {
            TableName = tableName;
        }

        public string TableName { get; protected set; }

        public string SchemaName { get { return Schema + '.' + TableName; } }

    }
}
