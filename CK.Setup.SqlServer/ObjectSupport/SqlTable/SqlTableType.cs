using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlTableType : IAmbiantContractDefiner
    {
        protected SqlTableType()
        {
        }

        protected SqlTableType( string tableName )
        {
            TableName = tableName;
        }

        [AmbiantProperty]
        public SqlDatabase Database { get; set; }

        public string TableName { get; protected set; }

        [AmbiantProperty]
        public string Schema { get; protected set; }

        public string SchemaName { get { return Schema + '.' + TableName; } }

    }
}
