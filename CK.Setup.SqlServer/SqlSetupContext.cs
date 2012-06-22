using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlSetupContext : ISetupDriverFactory, IDisposable
    {
        SqlManager _defaultDatabase;
        SqlManagerProvider _otherDatabases;

        public SqlSetupContext( string defaultDatabaseConnectionString, IActivityLogger logger )
        {
            _defaultDatabase = new SqlManager();
            _defaultDatabase.Logger = logger;
            _defaultDatabase.OpenFromConnectionString( defaultDatabaseConnectionString );
            _otherDatabases = new SqlManagerProvider( logger );
        }

        public SqlManager DefaultDatabase
        {
            get { return _defaultDatabase; }
        }

        public SqlManagerProvider OtherDatabases
        {
            get { return _otherDatabases; }
        }

        public IActivityLogger Logger
        {
            get { return _defaultDatabase.Logger; }
        }

        public virtual ItemDriver CreateDriver( Type driverType, ItemDriver.BuildInfo info )
        {
            if( driverType == typeof( SqlObjectDriver ) ) return new SqlObjectDriver( info, _defaultDatabase );
            return null;
        }

        public virtual ContainerDriver CreateDriverContainer( Type containerType, ContainerDriver.BuildInfo info )
        {
            if( containerType == typeof( ContainerDriver ) ) return new ContainerDriver( info );
            if( containerType == typeof( DatabaseSetupDriver ) ) return new DatabaseSetupDriver( info, DoObtainManager );
            return null;
        }

        SqlManager DoObtainManager( string dbName )
        {
            if( dbName == null ) throw new ArgumentNullException( "dbName" );
            if( dbName == Database.DefaultDatabaseName ) return _defaultDatabase;
            SqlManager m = ObtainManager( dbName );
            if( m == null ) Logger.Warn( "Database named '{0}' is not mapped.", dbName );
            return m;
        }

        protected virtual SqlManager ObtainManager( string dbName )
        {
            return _otherDatabases.Obtain( dbName );
        }


        public virtual void Dispose()
        {
            if( _defaultDatabase != null )
            {
                _defaultDatabase.Dispose();
                _defaultDatabase = null;
            }
        }

    }
}
