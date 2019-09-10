namespace CK.Core
{
    /// <summary>
    /// Typed <see cref="SqlDatabase"/> for the default <see cref="SqlDatabase"/>.
    /// </summary>
    public class SqlDefaultDatabase : SqlDatabase, IRealObject
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

        void StObjConstruct( string connectionString )
        {
            ConnectionString = connectionString;
        }
    }
}
