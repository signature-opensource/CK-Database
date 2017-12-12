using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Testing
{
    /// <summary>
    /// Support sql database related helpers.
    /// </summary>
    public interface ISqlServerTestHelperCore : ITestHelper
    {
        /// <summary>
        /// Gets the connection string to the master database from "SqlServer/MasterConnectionString" configuration.
        /// Defaults to "Server=.;Database=master;Integrated Security=SSPI".
        /// </summary>
        string MasterConnectionString { get; }

        /// <summary>
        /// Gets the database test name from "SqlServer/DatabaseTestName" from configuration.
        /// Defaults to the test project name where '.' are replaced with '_'.
        /// </summary>
        string DatabaseTestName { get; }

        /// <summary>
        /// Gets the connection string based on <see cref="MasterConnectionString"/> to the given database.
        /// </summary>
        /// <param name="dbName">Name of the database.</param>
        /// <returns>The connection string to the database.</returns>
        string GetConnectionString( string dbName );

        /// <summary>
        /// Gets the connection string to the <see cref="DatabaseTestName"/>.
        /// </summary>
        string DatabaseTestConnectionString { get; }

    }
}
