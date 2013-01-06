using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Associates a name to a connection string.
    /// </summary>
    public class SqlDatabaseDescriptor
    {
        /// <summary>
        /// Initializes a new <see cref="SqlDatabaseDescriptor"/>.
        /// </summary>
        /// <param name="name">Logical database name.</param>
        /// <param name="connectionString">Connection string to the database.</param>
        public SqlDatabaseDescriptor( string name, string connectionString )
        {
            DatabaseName = name;
            ConnectionString = connectionString;
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
    }
}
