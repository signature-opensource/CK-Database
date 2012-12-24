using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlTableAttribute : SqlPackageAttributeBase
    {
        public SqlTableAttribute( string tableName )
            : base( "CK.Setup.SqlServer.SqlTableAttributeImpl, CK.Setup.SqlServer" )
        {
            TableName = tableName;
        }

        public string TableName { get; set; }

    }
}
