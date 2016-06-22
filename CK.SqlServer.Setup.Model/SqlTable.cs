#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\SqlTable.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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

        public string SchemaName => Schema + '.' + TableName;

    }
}
