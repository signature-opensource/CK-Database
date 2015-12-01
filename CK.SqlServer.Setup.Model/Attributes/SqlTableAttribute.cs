#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\Attributes\SqlTableAttribute.cs) is part of CK-Database. 
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
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlTableAttribute : SqlPackageAttributeBase
    {
        public SqlTableAttribute( string tableName )
            : base( "CK.SqlServer.Setup.SqlTableAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            TableName = tableName;
        }

        public string TableName { get; set; }

    }
}
