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
    /// <summary>
    /// Attribute that must decorate a <see cref="SqlTable"/> class.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlTableAttribute : SqlPackageAttributeBase
    {
        /// <summary>
        /// Initializes a new <see cref="SqlTableAttribute"/>.
        /// </summary>
        /// <param name="tableName">The table name (with or without the <see cref="SqlPackageAttributeBase.Schema"/>).</param>
        public SqlTableAttribute( string tableName )
            : base( "CK.SqlServer.Setup.SqlTableAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            TableName = tableName;
        }

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        public string TableName { get; set; }

    }
}
