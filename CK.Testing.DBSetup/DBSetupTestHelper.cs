using System;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.Testing.DBSetup;
using CK.Testing.SqlServer;
using CK.Testing.StObjMap;
using CKSetup;

namespace CK.Testing
{
    /// <summary>
    /// Exposes standard implementation of <see cref="IDBSetupTestHelperCore"/>.
    /// </summary>
    public class DBSetupTestHelper : IDBSetupTestHelperCore
    {
        readonly ISetupableSetupTestHelper _setupableSetup;
        readonly ISqlServerTestHelper _sqlServer;

        internal DBSetupTestHelper( ISetupableSetupTestHelper setupableSetup, ISqlServerTestHelper sqlServer )
        {
            _setupableSetup = setupableSetup;
            _sqlServer = sqlServer;
            _setupableSetup.StObjSetupRunning += OnStObjSetupRunning;
            _setupableSetup.StObjMapAccessed += OnStObjMapAccessed;
            _setupableSetup.AutomaticServicesConfigured += OnAutomaticServicesConfigured;
        }

        void OnStObjMapAccessed( object sender, StObjMapAccessedEventArgs e )
        {
            if( e.DeltaLastAccessTime > TimeSpan.FromSeconds( 3 ) )
            {
                e.ShouldReset |= _sqlServer.EnsureDatabase();
            }
        }

        void OnStObjSetupRunning( object sender, StObjSetup.StObjSetupRunningEventArgs e )
        {
            if( !e.StObjEngineConfiguration.Aspects.Any( c => c is SqlSetupAspectConfiguration ) )
            {
                SqlSetupAspectConfiguration conf = new SqlSetupAspectConfiguration();
                conf.DefaultDatabaseConnectionString = _sqlServer.GetConnectionString();
                conf.IgnoreMissingDependencyIsError = true;
                conf.GlobalResolution = false;

                // If the database has been created, we force CKSetup to run the engine
                // even if the files are up to date.
                if( _sqlServer.EnsureDatabase() ) e.ForceSetup = ForceSetupLevel.Engine;
                e.StObjEngineConfiguration.Aspects.Add( conf );
            }
        }

        void OnAutomaticServicesConfigured( object sender, AutomaticServicesConfigurationEventArgs e )
        {
            var testConnectionString = _sqlServer.GetConnectionString();
            var defaultDB = e.StObjMap.StObjs.Obtain<SqlDefaultDatabase>();
            if( testConnectionString != defaultDB.ConnectionString )
            {

                _setupableSetup.Monitor.Trace( $"Replacing StObjMap's SqlDefaultDatabase connection string ({defaultDB.ConnectionString}) by the SqlServer test helper one: '{testConnectionString}'." );
                defaultDB.ConnectionString = testConnectionString;
            }
            else
            {
                _setupableSetup.Monitor.Trace( $"The StObjMap's SqlDefaultDatabase connection string is the one of the SqlServer test helper ({testConnectionString}). Nothing to change." );
            }
        }

        CKSetupRunResult IDBSetupTestHelperCore.RunDBSetup( ISqlServerDatabaseOptions db, bool traceStObjGraphOrdering, bool traceSetupGraphOrdering, bool revertNames )
        {
            return DoRunDBSetup( db, traceStObjGraphOrdering, traceSetupGraphOrdering, revertNames );
        }

        CKSetupRunResult DoRunDBSetup( ISqlServerDatabaseOptions db, bool traceStObjGraphOrdering, bool traceSetupGraphOrdering, bool revertNames )
        {
            if( db == null ) db = _sqlServer.DefaultDatabaseOptions;
            using( _setupableSetup.Monitor.OpenInfo( $"Running DBSetup on {db}." ) )
            {
                try
                {
                    var stObjConf = StObjSetupTestHelper.CreateDefaultConfiguration( _setupableSetup );

                    // If the database has been created, we force CKSetup to run the engine
                    // even if the files are up to date.
                    if( _sqlServer.EnsureDatabase() ) stObjConf.ForceSetup = ForceSetupLevel.Engine;

                    stObjConf.Configuration.TraceDependencySorterInput = traceStObjGraphOrdering;
                    stObjConf.Configuration.TraceDependencySorterOutput = traceStObjGraphOrdering;

                    var setupable = new SetupableAspectConfiguration();
                    setupable.RevertOrderingNames = revertNames;
                    setupable.TraceDependencySorterInput = traceSetupGraphOrdering;
                    setupable.TraceDependencySorterOutput = traceSetupGraphOrdering;
                    stObjConf.Configuration.Aspects.Add( setupable );

                    var sqlServer = new SqlSetupAspectConfiguration();
                    sqlServer.DefaultDatabaseConnectionString = _sqlServer.GetConnectionString( db.DatabaseName );
                    sqlServer.GlobalResolution = false;
                    sqlServer.IgnoreMissingDependencyIsError = true;
                    stObjConf.Configuration.Aspects.Add( sqlServer );

                    return _setupableSetup.RunStObjSetup( stObjConf.Configuration, stObjConf.ForceSetup ); 
                }
                catch( Exception ex )
                {
                    _setupableSetup.Monitor.Error( ex );
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IDBSetupTestHelper"/> default implementation.
        /// </summary>
        public static IDBSetupTestHelper TestHelper => TestHelperResolver.Default.Resolve<IDBSetupTestHelper>();

    }
}
