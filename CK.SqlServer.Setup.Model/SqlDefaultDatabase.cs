using Microsoft.Extensions.Configuration;

namespace CK.Core;

/// <summary>
/// Default <see cref="SqlDatabase"/>.
/// </summary>
public class SqlDefaultDatabase : SqlDatabase
{
    /// <summary>
    /// Initializes the default database. Its name is <see cref="SqlDatabase.DefaultDatabaseName"/>
    /// and <see cref="SqlDatabase.DefaultSchemaName"/> is registered.
    /// </summary>
    public SqlDefaultDatabase()
        : base( DefaultDatabaseName )
    {
        EnsureSchema( DefaultSchemaName );
    }

    // The run time default connection string: "SqlServer:DefaultDatabaseConnectionString" of the host, projected by the
    // "SqlServer" aspect of the builder (SqlServerAspect).
    void ConfigureServices( StObjContextRoot.ServiceRegister r, SqlDefaultDatabase defaultDatabase, IConfiguration? configuration = null )
    {
        var cs = configuration?["SqlServer:DefaultDatabaseConnectionString"];
        if( cs != null )
        {
            defaultDatabase.ConnectionString = cs;
        }
    }
}
