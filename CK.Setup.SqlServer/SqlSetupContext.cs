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
        //List<TypedObjectHandler> _typedObjectHandlers;
        List<string> _ignoredAssemblies;

        public SqlSetupContext( string defaultDatabaseConnectionString, IActivityLogger logger )
        {
            _defaultDatabase = new SqlManager();
            _defaultDatabase.Logger = logger;
            _defaultDatabase.OpenFromConnectionString( defaultDatabaseConnectionString );
            _otherDatabases = new SqlManagerProvider( logger );
            //_typedObjectHandlers = new List<TypedObjectHandler>();
            _ignoredAssemblies = new List<string>();

            //_typedObjectHandlers.Add( new SqlTypedObjectStandardHandler() );

            _ignoredAssemblies.Add( "System" );
            _ignoredAssemblies.Add( "System.Core" );
            _ignoredAssemblies.Add( "System.Data" );
            _ignoredAssemblies.Add( "System.Data.DataSetExtensions" );
            _ignoredAssemblies.Add( "System.Data.Xml" );
            _ignoredAssemblies.Add( "System.Data.Xml.Linq" );
            
            _ignoredAssemblies.Add( "CK.Core" );
            _ignoredAssemblies.Add( "CK.Setup" );
            _ignoredAssemblies.Add( "CK.SqlServer" );

            _ignoredAssemblies.Add( "Microsoft.CSharp" );
            _ignoredAssemblies.Add( "Microsoft.Practices.ServiceLocation" );
            _ignoredAssemblies.Add( "Microsoft.Practices.Unity" );
            _ignoredAssemblies.Add( "Microsoft.Practices.Unity.Configuration" );

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

        //public IList<TypedObjectHandler> TypedObjectHandlers
        //{
        //    get { return _typedObjectHandlers; }
        //}

        public bool AutomaticAssemblyDiscovering { get; set; }

        public IList<string> IgnoredAssemblyNames
        {
            get { return _ignoredAssemblies; }
        }

        public virtual ItemDriver CreateDriver( Type driverType, ItemDriver.BuildInfo info )
        {
            if( driverType == typeof( SqlObjectDriver ) ) return new SqlObjectDriver( info, _defaultDatabase );
            if( driverType == typeof( SqlConnectionSetupDriver ) ) return new SqlConnectionSetupDriver( info, DoObtainManager );
            return null;
        }

        public virtual ContainerDriver CreateDriverContainer( Type containerType, ContainerDriver.BuildInfo info )
        {
            if( containerType == typeof( ContainerDriver ) ) return new ContainerDriver( info );
            if( containerType == typeof( PackageDriver ) ) return new PackageDriver( info );
            if( containerType == typeof( SqlDatabaseSetupDriver ) ) return new SqlDatabaseSetupDriver( info );
            return null;
        }

        SqlManager DoObtainManager( string dbName )
        {
            if( dbName == null ) throw new ArgumentNullException( "dbName" );
            if( dbName == SqlDatabase.DefaultDatabaseName ) return _defaultDatabase;
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
