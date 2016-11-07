using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Offers script execution facility.
    /// </summary>
    public interface ISqlManager : ISqlManagerBase
    {
        /// <summary>
        /// Gets the <see cref="SqlConnectionProvider"/> of this <see cref="ISqlManager"/>.
        /// </summary>
        SqlConnectionProvider Connection { get; }

        /// <summary>
        /// True if the connection to the current database is opened. Can be called on a 
        /// disposed <see cref="ISqlManager"/>.
        /// </summary>
        bool IsOpen();
        
        /// <summary>
        /// Tries to remove all objects from a given schema.
        /// </summary>
        /// <param name="schemaName">Name of the schema. Must not be null nor empty.</param>
        /// <param name="dropSchema">True to drop the schema itself.</param>
        /// <returns>True on success, false otherwise.</returns>
        bool SchemaDropAllObjects( string schemaName, bool dropSchema );
    }
}
