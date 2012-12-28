using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseSetupDriver : SetupDriver
    {
        readonly SqlDatabaseConnectionSetupDriver _connection;

        public SqlDatabaseSetupDriver( BuildInfo info )
            : base( info )
        {
            _connection = (SqlDatabaseConnectionSetupDriver)DirectDependencies[Item.ConnectionItem];
        }

        /// <summary>
        /// Masked Item to formally be associated to a <see cref="SqlDatabaseItem"/> item.
        /// </summary>
        public new SqlDatabaseItem Item { get { return (SqlDatabaseItem)base.Item; } }

    }
}
