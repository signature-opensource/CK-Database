using CK.Core;
using CK.Setup;
using CK.Testing.SqlServer;
using static CK.Testing.SqlServerTestHelper;

namespace CK.Testing
{

    /// <summary>
    /// Provides EngineConfiguration.EnsureSqlServerConfigurationAspect() helper.
    /// </summary>
    public static class SqlServerEngineTestHelperExtensions
    {
        /// <summary>
        /// Adds or configures the <see cref="SetupableAspectConfiguration"/> and <see cref="SqlSetupAspectConfiguration"/> in the
        /// <see cref="EngineConfiguration.Aspects"/>.
        /// <para>
        /// The database is created if it doesn't exist.
        /// </para>
        /// </summary>
        /// <param name="engineConfiguration">This engine configuration to configure.</param>
        /// <param name="databaseOptions">Optional <see cref="ISqlServerDatabaseOptions"/>.</param>
        /// <param name="resetDatabase">True to reset the database even if its options match the <paramref name="databaseOptions"/>.</param>
        /// <param name="revertOrderingName">
        /// By default, the topological sort of the real objects and setupable items graphs randomly sort the
        /// items in the same rank with their ascending or descending names. This helps find missing constraints
        /// in the graphs.
        /// <para>
        /// To disable the random behavior, set this to false or true.
        /// </para>
        /// </param>
        public static void EnsureSqlServerConfigurationAspect( this EngineConfiguration engineConfiguration,
                                                               ISqlServerDatabaseOptions? databaseOptions = null,
                                                               bool resetDatabase = false,
                                                               bool? revertOrderingName = null )
        {
            bool revertOrdering = revertOrderingName ?? (Environment.TickCount % 2) == 0;
            if( revertOrdering )
            {
                TestHelper.Monitor.Info( "Reverting ordering names in both real objects and setupable items graphs." );
            }
            engineConfiguration.RevertOrderingNames = revertOrdering;
            var setupable = engineConfiguration.EnsureAspect<SetupableAspectConfiguration>();
            setupable.RevertOrderingNames = revertOrdering;

            var sqlEngine = engineConfiguration.Aspects.OfType<SqlSetupAspectConfiguration>().FirstOrDefault();
            if( sqlEngine == null )
            {
                sqlEngine = new SqlSetupAspectConfiguration();
                sqlEngine.IgnoreMissingDependencyIsError = true;
                sqlEngine.GlobalResolution = false;
                engineConfiguration.AddAspect( sqlEngine );
            }
            sqlEngine.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( databaseOptions?.DatabaseName );
            TestHelper.EnsureDatabase( databaseOptions, resetDatabase );
        }
    }
}
