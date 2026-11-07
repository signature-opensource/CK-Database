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

    // Temporary workaround, waiting for the Unified Configuration.
    void ConfigureServices( StObjContextRoot.ServiceRegister r, SqlDefaultDatabase defaultDatabase, IConfiguration? configuration = null )
    {
        var cs = configuration?["AppIdentity:Local:SqlServer:DefaultDatabaseConnectionString"];
        if( cs != null )
        {
            defaultDatabase.ConnectionString = cs;
        }
    }
}
