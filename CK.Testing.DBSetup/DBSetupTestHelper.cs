using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CK.Core;
using CK.Setup;
using CK.Testing.DBSetup;
using CK.Testing.SqlServer;
using CK.Text;
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

        internal DBSetupTestHelper( ITestHelperConfiguration config, ISetupableSetupTestHelper setupableSetup, ISqlServerTestHelper sqlServer )
        {
            _setupableSetup = setupableSetup;
            _sqlServer = sqlServer;
            _setupableSetup.StObjSetupRunning += OnStObjSetupRunning;
        }

        void OnStObjSetupRunning( object sender, StObjSetup.StObjSetupRunningEventArgs e )
        {
            if( !e.StObjEngineConfiguration.Aspects.Any( c => c is SqlSetupAspectConfiguration ) )
            {
                SqlSetupAspectConfiguration conf = new SqlSetupAspectConfiguration();
                conf.DefaultDatabaseConnectionString = _sqlServer.GetConnectionString();
                conf.IgnoreMissingDependencyIsError = true;
                conf.GlobalResolution = false;

                e.ForceSetup |= _sqlServer.EnsureDatabase();
                e.StObjEngineConfiguration.Aspects.Add( conf );
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

                    stObjConf.ForceSetup |= _sqlServer.EnsureDatabase( db );

                    var setupable = new SetupableAspectConfiguration();
                    setupable.RevertOrderingNames = revertNames;
                    setupable.TraceDependencySorterInput = traceSetupGraphOrdering;
                    setupable.TraceDependencySorterInput = traceSetupGraphOrdering;
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
