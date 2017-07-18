using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    public class SqlSetupAspect : ISetupEngineAspect, ISqlSetupAspect, IDisposable
    {
        readonly SqlSetupAspectConfiguration _config;
        readonly SetupEngine _engine;
        readonly SqlManagerProvider _databases;
        ISqlServerParser _sqlParser;
        ISqlManagerBase _defaultDatabase;

        class ConfiguratorHook : SetupEngineConfigurator
        {
            readonly SqlSetupAspect _center;

            public ConfiguratorHook( SqlSetupAspect sqlAspect )
                : base( sqlAspect._engine.SetupableConfigurator )
            {
                _center = sqlAspect;
            }

            public override void ResolveParameterValue( IActivityMonitor monitor, IStObjFinalParameter parameter )
            {
                base.ResolveParameterValue( monitor, parameter );
                if( parameter.Name == "connectionString" )
                {
                    SqlDatabase db = parameter.Owner.InitialObject as SqlDatabase;
                    if( db != null )
                    {
                        parameter.SetParameterValue( _center._config.FindConnectionStringByName( db.Name ) );
                    }
                }
            } 
        }

        /// <summary>
        /// Initializes a new <see cref="SqlSetupAspect"/>.
        /// This constructor is called by the <see cref="SetupEngine"/> whenever a <see cref="SqlSetupAspectConfiguration"/> configuration object
        /// appears in <see cref="SetupEngineConfiguration.Aspects"/> list.
        /// </summary>
        /// <param name="engine">Current engine.</param>
        /// <param name="config">Configuration object.</param>
        public SqlSetupAspect( SetupEngine engine, SqlSetupAspectConfiguration config )
        {
            if( engine == null ) throw new ArgumentNullException( "engine" );
            if( config == null ) throw new ArgumentNullException( "config" );
            _config = config;
            _engine = engine;
            _databases = new SqlManagerProvider( _engine.Monitor, m => m.IgnoreMissingDependencyIsError = _config.IgnoreMissingDependencyIsError );
            _databases.Add( SqlDatabase.DefaultDatabaseName, _config.DefaultDatabaseConnectionString, autoCreate:true );
            foreach( var db in _config.Databases )
            {
                _databases.Add( db.LogicalDatabaseName, db.ConnectionString, db.AutoCreate );
            }
        }

        bool ISetupEngineAspect.Configure()
        {
            _defaultDatabase = _databases.FindManagerByName( SqlDatabase.DefaultDatabaseName );
            ISqlManager realSqlManager = _defaultDatabase as ISqlManager;
            if( _engine.StartConfiguration.VersionedItemReader == null )
            {
                if( realSqlManager != null )
                {
                    _engine.Monitor.Info().Send( $"Setting SqlVersionedItemReader on the default database as the version reader." );
                    _engine.StartConfiguration.VersionedItemReader = new SqlVersionedItemReader( realSqlManager );
                }
                else
                {
                    _engine.Monitor.Info().Send( $"SqlSetupAspects: Unable to use SqlVersionedItemReader on the default database as the version writer since the underlying sql manager is not a real manager." );
                }
            }
            if( _engine.StartConfiguration.VersionedItemWriter == null )
            {
                _engine.Monitor.Info().Send( $"Setting SqlVersionedItemWriter on the default database as the version writer." );
                _engine.StartConfiguration.VersionedItemWriter = new SqlVersionedItemWriter( _defaultDatabase );
            }
            if( _engine.StartConfiguration.SetupSessionMemoryProvider == null )
            {

                if( realSqlManager != null )
                {
                    _engine.Monitor.Info().Send( $"Setting SqlSetupSessionMemoryProvider on the default database as the memory provider." );
                    _engine.StartConfiguration.SetupSessionMemoryProvider = new SqlSetupSessionMemoryProvider( realSqlManager );
                }
                else
                {
                    _engine.Monitor.Info().Send( $"SqlSetupAspects: Unable to use SqlSetupSessionMemoryProvider on the default database as the memory provider since the underlying sql manager is not a real manager." );
                }
            }

            _engine.SetupableConfigurator = new ConfiguratorHook( this );
            _engine.StartConfiguration.AddExplicitRegisteredClass( typeof( SqlDefaultDatabase ) );
            return true;
        }

        /// <summary>
        /// Gets the engine to which this aspect is bound.
        /// </summary>
        public SetupEngine SetupEngine => _engine; 

        ISetupEngineAspectConfiguration ISetupEngineAspect.Configuration => _config; 

        /// <summary>
        /// Gets the configuration object.
        /// </summary>
        public SqlSetupAspectConfiguration Configuration => _config;

        /// <summary>
        /// Gets whether the resolution of objects must be done globally.
        /// This is a temporary property: this should eventually be the only mode...
        /// </summary>
        public bool GlobalResolution => _config.GlobalResolution;

        /// <summary>
        /// Gets the <see cref="ISqlServerParser"/> to use.
        /// </summary>
        public ISqlServerParser SqlParser
        {
            get
            {
                if( _sqlParser == null )
                {
                    Type t = SimpleTypeFinder.WeakResolver( "CK.SqlServer.Parser.SqlServerParser, CK.SqlServer.Parser", true );
                    _sqlParser = (ISqlServerParser)Activator.CreateInstance( t );
                }
                return _sqlParser;
            }
        }

        /// <summary>
        /// Gets the default database as a <see cref="ISqlManagerBase"/> object.
        /// </summary>
        public ISqlManagerBase DefaultSqlDatabase => _defaultDatabase; 

        /// <summary>
        /// Gets the available databases (including the <see cref="DefaultSqlDatabase"/>).
        /// It is initialized with <see cref="SqlSetupAspectConfiguration.Databases"/> content.
        /// </summary>
        public ISqlManagerProvider SqlDatabases => _databases; 

        /// <summary>
        /// Releases all database managers.
        /// Can safely be called multiple times.
        /// </summary>
        public virtual void Dispose()
        {
            // Can safely be called multiple times.
            _databases.Dispose();
        }

    }
}
