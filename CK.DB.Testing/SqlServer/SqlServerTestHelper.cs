using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CK.Core;
using CK.Text;

namespace CK.Testing
{

    /// <summary>
    /// Provides default implementation of <see cref="ISqlServerTestHelperCore"/>.
    /// </summary>
    public class SqlServerTestHelper : ISqlServerTestHelperCore
    {
        readonly ITestHelperConfiguration _config;
        readonly IMonitorTestHelper _monitor;
        readonly string _databaseTestName;
        SqlConnectionStringBuilder _masterConnectionString;

        public SqlServerTestHelper( ITestHelperConfiguration config, IMonitorTestHelper monitor )
        {
            _config = config;
            _monitor = monitor;
            _databaseTestName = config.Get( "SqlServer/DatabaseTestName" ) ?? monitor.TestProjectName.Replace( '.', '_' );
        }
        string ISqlServerTestHelperCore.MasterConnectionString => EnsureMasterConnection().InitialCatalog;

        string ISqlServerTestHelperCore.DatabaseTestName => _databaseTestName;

        string ISqlServerTestHelperCore.DatabaseTestConnectionString => DoGetConnectionString( _databaseTestName );

        string ISqlServerTestHelperCore.GetConnectionString( string dbName ) => DoGetConnectionString( dbName );

        string DoGetConnectionString( string dbName )
        {
            var c = EnsureMasterConnection();
            string savedMaster = c.InitialCatalog;
            c.InitialCatalog = dbName;
            string result = c.ToString();
            c.InitialCatalog = savedMaster;
            return result;
        }

        SqlConnectionStringBuilder EnsureMasterConnection()
        {
            if( _masterConnectionString == null )
            {
                string c = _config.Get( "SqlServer/MasterConnectionString" );
                if( c == null )
                {
                    c = "Server=.;Database=master;Integrated Security=SSPI";
                    _monitor.Monitor.Info( $"Using default connection string: {c}" );
                }
                _masterConnectionString = new SqlConnectionStringBuilder( c );
            }
            return _masterConnectionString;
        }

        /// <summary>
        /// Gets the <see cref="ISqlServerTestHelper"/> default implementation.
        /// </summary>
        public static ISqlServerTestHelper TestHelper { get; } = TestHelperResolver.Default.Resolve<ISqlServerTestHelper>();

    }
}
