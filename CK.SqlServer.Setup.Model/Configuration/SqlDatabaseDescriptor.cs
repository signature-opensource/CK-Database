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
using System.Xml.Linq;
using CK.Core;

namespace CK.Setup
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
            LogicalDatabaseName = name;
            ConnectionString = connectionString;
            AutoCreate = autoCreate;
        }

        /// <summary>
        /// Initializes a new <see cref="SqlDatabaseDescriptor"/> from its xml representation.
        /// </summary>
        /// <param name="e">The element.</param>
        public SqlDatabaseDescriptor( XElement e )
        {
            XName xLogicalDatabaseName = XNamespace.None + "LogicalDatabaseName";
            XName xConnectionString = XNamespace.None + "ConnectionString";
            XName xAutoCreate = XNamespace.None + "AutoCreate";

            LogicalDatabaseName = e.Element( xLogicalDatabaseName ).Value;
            ConnectionString = e.Element( xConnectionString ).Value;
            AutoCreate = string.Equals( e.Element( xAutoCreate )?.Value, "true", StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Gets or sets the logical name of the database.
        /// It is independent of the actual database name.
        /// </summary>
        public string LogicalDatabaseName { get; set; }

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
