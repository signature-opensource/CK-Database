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
    /// <summary>
    /// Base class for table objects. 
    /// Unless marked with <see cref="IAmbientDefiner{T}"/>, direct specializations are de facto ambient objects.
    /// A table is a <see cref="SqlPackageBase"/> with a <see cref="TableName"/>.
    /// </summary>
    public class SqlTable : SqlPackageBase, IAmbientObject, IAmbientDefiner<SqlTable>
    {
        /// <summary>
        /// Initializes a new <see cref="SqlTable"/> with a null <see cref="TableName"/>.
        /// </summary>
        protected SqlTable()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SqlTable"/> with a <see cref="TableName"/>.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        public SqlTable( string tableName )
        {
            TableName = tableName;
        }

        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string TableName { get; protected set; }

        /// <summary>
        /// Gets the schema.name full name of the table.
        /// </summary>
        public string SchemaName => Schema + '.' + TableName;

    }
}
