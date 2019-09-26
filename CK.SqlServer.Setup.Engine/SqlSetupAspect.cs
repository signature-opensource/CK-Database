using System;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Implements <see cref="ISqlSetupAspect"/>.
    /// </summary>
    public class SqlSetupAspect : IStObjEngineAspect, ISqlSetupAspect, IDisposable
    {
        readonly SqlSetupAspectConfiguration _config;
        readonly ISetupableAspectRunConfiguration _setupConfiguration;
        readonly SqlManagerProvider _databases;
        ISqlServerParser _sqlParser;
        ISqlManagerBase _defaultDatabase;

        class StObjConfiguratorHook : StObjConfigurationLayer
        {
            readonly SqlSetupAspectConfiguration _config;

            public StObjConfiguratorHook( SqlSetupAspectConfiguration config )
            {
                _config = config;
            }

            public override void ResolveParameterValue( IActivityMonitor monitor, IStObjFinalParameter parameter )
            {
                base.ResolveParameterValue( monitor, parameter );
                if( parameter.Name == "connectionString"
                    && parameter.Owner.InitialObject is SqlDatabase db )
                {
                    parameter.SetParameterValue( _config.FindConnectionStringByName( db.Name ) );
                }
            } 
        }

        /// <summary>
        /// Initializes a new <see cref="SqlSetupAspect"/>.
        /// This constructor is called by the StObjEngine whenever a <see cref="SqlSetupAspectConfiguration"/> configuration object
        /// appears in <see cref="StObjEngineConfiguration.Aspects"/> list.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="setupConfiguration"></param>
        public SqlSetupAspect( SqlSetupAspectConfiguration config, IActivityMonitor monitor, ConfigureOnly<ISetupableAspectRunConfiguration> setupConfiguration )
        {
            if( setupConfiguration.Service == null ) throw new ArgumentNullException( nameof(setupConfiguration) );
            if( config == null ) throw new ArgumentNullException( nameof(config) );
            _config = config;
            _setupConfiguration = setupConfiguration.Service;
            _databases = new SqlManagerProvider( monitor, m => m.IgnoreMissingDependencyIsError = _config.IgnoreMissingDependencyIsError );
            _databases.Add( SqlDatabase.DefaultDatabaseName, _config.DefaultDatabaseConnectionString, autoCreate:true );
            foreach( var db in _config.Databases )
            {
                _databases.Add( db.LogicalDatabaseName, db.ConnectionString, db.AutoCreate );
            }
        }

        bool IStObjEngineAspect.Configure( IActivityMonitor monitor, IStObjEngineConfigureContext context )
        {
            _defaultDatabase = _databases.FindManagerByName( SqlDatabase.DefaultDatabaseName );
            ISqlManager realSqlManager = _defaultDatabase as ISqlManager;
            if( !context.ServiceContainer.IsAvailable<IVersionedItemReader>() )
            {
                if( realSqlManager != null )
                {
                    monitor.Info( "Registering SqlVersionedItemReader on the default database as the version reader." );
                    context.ServiceContainer.Add<IVersionedItemReader>( new SqlVersionedItemReader( realSqlManager ) );
                }
                else
                {
                    monitor.Info( $"Unable to use SqlVersionedItemReader on the default database as the version writer since the underlying sql manager is not a real manager." );
                }
            }
            if( !context.ServiceContainer.IsAvailable<IVersionedItemWriter>() )
            {
                monitor.Info( "Registering SqlVersionedItemWriter on the default database as the version writer." );
                context.ServiceContainer.Add<IVersionedItemWriter>( new SqlVersionedItemWriter( _defaultDatabase ) );
            }
            if( !context.ServiceContainer.IsAvailable<ISetupSessionMemoryProvider>() )
            {
                if( realSqlManager != null )
                {
                    monitor.Info( $"Registering SqlSetupSessionMemoryProvider on the default database as the memory provider." );
                    context.ServiceContainer.Add<ISetupSessionMemoryProvider>( new SqlSetupSessionMemoryProvider( realSqlManager ) );
                }
                else
                {
                    monitor.Info( "Unable to use SqlSetupSessionMemoryProvider on the default database as the memory provider since the underlying sql manager is not a real manager." );
                }
            }
            _sqlParser = new SqlServerParser();
            context.ServiceContainer.Add( _sqlParser );
            context.ServiceContainer.Add<ISqlManagerProvider>( _databases );
            context.Configurator.AddLayer( new StObjConfiguratorHook( _config ) );
            context.AddExplicitRegisteredType( typeof( SqlDefaultDatabase ) );
            return true;
        }

        bool IStObjEngineAspect.Run( IActivityMonitor monitor, IStObjEngineRunContext context ) => true;

        bool IStObjEngineAspect.Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context ) => true;

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
        public ISqlServerParser SqlParser => _sqlParser;

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
