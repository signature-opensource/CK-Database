namespace CK.SqlServer.Setup;


/// <summary>
/// Providers for ready to use <see cref="ISqlManagerBase"/> must offer two ways to access 
/// them: by name and by connection string.
/// </summary>
public interface ISqlManagerProvider
{
    /// <summary>
    /// Gets an opened, ready to use, <see cref="ISqlManagerBase"/> by its logical name (null if not found).
    /// </summary>
    /// <param name="logicalName">Name of the sql connection.</param>
    /// <returns>The manager or null if not found.</returns>
    ISqlManagerBase FindManagerByName( string logicalName );

    /// <summary>
    /// Gets an opened, ready to use, <see cref="ISqlManagerBase"/> by its connection string (null if not found).
    /// </summary>
    /// <param name="connectionString">Connection string to the database.</param>
    /// <returns>The manager or null if not found.</returns>
    ISqlManagerBase FindManagerByConnectionString( string connectionString );
}
