using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlSetupContext : ISqlManagerProvider, IDisposable
    {
        SqlManager _defaultDatabase;
        SqlManagerProvider _databases;
        AssemblyRegistererConfiguration _regConf;
        StObjConfigurator _stObjConfigurator;
        List<Type> _regTypeList;

        /// <summary>
        /// Initializes a new <see cref="SqlSetupContext"/> on an existing database.
        /// </summary>
        /// <param name="defaultDatabaseConnectionString">
        /// Connection string to an existing database. The <see cref="DefaultSqlDatabase"/> connection will 
        /// be opened by default.</param>
        /// <param name="logger">Logger to use for the whole process.</param>
        public SqlSetupContext( string defaultDatabaseConnectionString, IActivityLogger logger )
            : this( logger )
        {
            _databases.Add( SqlDatabase.DefaultDatabaseName, defaultDatabaseConnectionString );
            _defaultDatabase = _databases.FindManagerByName( SqlDatabase.DefaultDatabaseName );
        }

        /// <summary>
        /// Initializes a new <see cref="SqlSetupContext"/>, using an existing and ready <see cref="SqlManager"/> as 
        /// the <see cref="DefaultSqlDatabase"/>.
        /// </summary>
        /// <param name="defaultDatabase">Default database. Must be opened and the database must exist.</param>
        public SqlSetupContext( SqlManager defaultDatabase )
            : this( defaultDatabase.Logger )
        {
            if( defaultDatabase == null ) throw new ArgumentNullException( "defaultDatabase" );
            if( !defaultDatabase.IsOpen() ) throw new ArgumentException( "Database manager must be opened.", "defaultDatabase" );
            _databases.AddDefaultDatabase( defaultDatabase );
            _defaultDatabase = defaultDatabase;
        }

        private SqlSetupContext( IActivityLogger logger )
        {
            _databases = new SqlManagerProvider( logger );
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

        SqlManager ISqlManagerProvider.FindManagerByName( string dbName )
        {
            if( dbName == null ) throw new ArgumentNullException( "dbName" );
            if( dbName == SqlDatabase.DefaultDatabaseName ) return _defaultDatabase;
            SqlManager m = ObtainManager( dbName );
            if( m == null ) Logger.Warn( "Database named '{0}' is not mapped.", dbName );
            return m;
        }

        SqlManager ISqlManagerProvider.FindManagerByConnectionString( string conString )
        {
            if( conString == null ) throw new ArgumentNullException( "conString" );
            if( conString == _defaultDatabase.Connection.ConnectionString ) return _defaultDatabase;
            SqlManager m = ObtainManagerByConnectionString( conString );
            if( m == null ) Logger.Warn( "Database connection to '{0}' is not mapped.", conString );
            return m;
        }

        protected virtual SqlManager ObtainManager( string dbName )
        {
            return _databases.FindManagerByName( dbName );
        }

        protected virtual SqlManager ObtainManagerByConnectionString( string conString )
        {
            return _databases.FindManagerByConnectionString( conString );
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
