#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\Configuration\SqlDatabaseDescriptor.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Associates a name to a connection string.
    /// </summary>
    [Serializable]
    public class SqlDatabaseDescriptor
    {
        /// <summary>
        /// Initializes a new <see cref="SqlDatabaseDescriptor"/>.
        /// </summary>
        /// <param name="name">Logical database name.</param>
        /// <param name="connectionString">Connection string to the database.</param>
        /// <param name="autoCreate">Whether the database should be created if opening the connection fails.</param>
        public SqlDatabaseDescriptor( string name, string connectionString, bool autoCreate = true )
        {
            DatabaseName = name;
            ConnectionString = connectionString;
            AutoCreate = autoCreate;
        }

        /// <summary>
        /// Gets or sets the logical name of the database.
        /// It is independant of the actual database name.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets whether the database should be created
        /// if opening the connection fails.
        /// </summary>
        public bool AutoCreate { get; set; }
    }
}
