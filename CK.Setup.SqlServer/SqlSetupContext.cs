using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;

namespace CK.Setup.SqlServer
{


    public class SqlSetupContext : ISetupDriverFactory, ISqlManagerProvider, IDisposable
    {
        SqlManager _defaultDatabase;
        SqlManagerProvider _databases;
        AssemblyRegistererConfiguration _regConf;
        StObjConfigurator _stObjConfigurator;
        List<Type> _regTypeList;

        public SqlSetupContext( string defaultDatabaseConnectionString, IActivityLogger logger )
        {
            _databases = new SqlManagerProvider( logger );
            _databases.Add( SqlDatabase.DefaultDatabaseName, defaultDatabaseConnectionString );
            _defaultDatabase = _databases.FindManager( SqlDatabase.DefaultDatabaseName );
            _stObjConfigurator = new StObjConfigurator();
            _regConf = new AssemblyRegistererConfiguration();
            _regTypeList = new List<Type>();
            _regTypeList.Add( typeof( SqlDefaultDatabase ) );
        }

        public AssemblyRegistererConfiguration AssemblyRegistererConfiguration
        {
            get { return _regConf; }
        }

        /// <summary>
        /// Gets a list of class types that will be explicitely registered (even if they belong to
        /// a assembly that is not discovered or appears in <see cref="AssemblyRegistererConfiguration.IgnoredAssemblyNames"/>).
        /// </summary>
        public IList<Type> ExplicitRegisteredClasses
        {
            get { return _regTypeList; }
        }

        public IActivityLogger Logger
        {
            get { return _defaultDatabase.Logger; }
        }

        public virtual SetupDriver CreateDriver( Type driverType, SetupDriver.BuildInfo info )
        {
            if( driverType == typeof( SqlObjectDriver ) ) return new SqlObjectDriver( info, this );
            if( driverType == typeof( SetupDriver ) ) return new SetupDriver( info );
            if( driverType == typeof( SqlDatabaseSetupDriver ) ) return new SqlDatabaseSetupDriver( info, this );
            return null;
        }

        public StObjConfigurator StObjConfigurator
        {
            get { return _stObjConfigurator; }
        }

        public SqlManager DefaultSqlDatabase
        {
            get { return _defaultDatabase; }
        }

        public SqlManagerProvider SqlDatabases
        {
            get { return _databases; }
        }

        SqlManager ISqlManagerProvider.FindManager( string dbName )
        {
            if( dbName == null ) throw new ArgumentNullException( "dbName" );
            if( dbName == SqlDatabase.DefaultDatabaseName ) return _defaultDatabase;
            SqlManager m = ObtainManager( dbName );
            if( m == null ) Logger.Warn( "Database named '{0}' is not mapped.", dbName );
            return m;
        }

        protected virtual SqlManager ObtainManager( string dbName )
        {
            return _databases.FindManager( dbName );
        }

        public virtual void Dispose()
        {
            if( _databases != null )
            {
                _databases.Dispose();
                _databases = null;
            }
        }

    }
}
