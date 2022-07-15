using System;
using System.Diagnostics;
using System.Linq;
using CK.Core;
using CK.CodeGen;
using CK.Setup;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Implements <see cref="ISqlSetupAspect"/>.
    /// </summary>
    public sealed class SqlSetupAspect : IStObjEngineAspect, ISqlSetupAspect, IDisposable
    {
        readonly SqlSetupAspectConfiguration _config;
        readonly SqlManagerProvider _databases;
        ISqlServerParser _sqlParser;
        ISqlManagerBase _defaultDatabase;

        sealed class StObjConfiguratorHook : StObjConfigurationLayer
        {
            readonly SqlSetupAspectConfiguration _config;

            public StObjConfiguratorHook( SqlSetupAspectConfiguration config )
            {
                _config = config;
            }

            public override void Configure( IActivityMonitor monitor, IStObjMutableItem o )
            {
                base.Configure( monitor, o );
                // Magic!
                // We call the SqlDatabase.StObjConstruct (the one of the base SqlDatabase "slice"), with the 
                // parameters from the aspect configuration based on the instance Name.
                if( o.InitialObject is SqlDatabase db && o.Generalization == null )
                {
                    var fromAbove = o.ConstructParametersAboveRoot;
                    Debug.Assert( fromAbove != null, "Since we are on the root of the specializations path." );
                    var (t, parameters) = fromAbove.Single();
                    if( parameters.Count != 3
                        || parameters[0].Name != "connectionString"
                        || parameters[0].Type != typeof( string )
                        || parameters[1].Name != "hasCKCore"
                        || parameters[1].Type != typeof( bool )
                        || parameters[2].Name != "useSnapshotIsolation"
                        || parameters[2].Type != typeof( bool ) )
                    {
                        throw new CKException( "Expected SqlDatabase.StObjContruct(string? connectionString = null, bool hasCKCore = false, bool useSnapshotIsolation = false)" );
                    }
                    if( db.IsDefaultDatabase )
                    {
                        monitor.Debug( $"SqlDefaultDatabase.ConnectionString configured to '{_config.DefaultDatabaseConnectionString}' when StObjConstruct will be called." );
                        parameters[0].SetParameterValue( _config.DefaultDatabaseConnectionString );
                        // This is useless since the parameters have default values.
                        // parameters[1].SetParameterValue( true );
                        // parameters[2].SetParameterValue( true );
                    }
                    else
                    {
                        var desc = _config.Databases.Find( d => d.LogicalDatabaseName == db.Name );
                        if( desc != null )
                        {
                            monitor.Info( $"Database named '{db.Name}' of type '{db.GetType().FullName}' configured to: ConnectionString='{desc.ConnectionString}', HasCKCore={desc.HasCKCore}, UseSnapshotIsolation={desc.UseSnapshotIsolation} when StObjConstruct will be called." );
                            parameters[0].SetParameterValue( desc.ConnectionString );
                            parameters[1].SetParameterValue( desc.HasCKCore );
                            parameters[2].SetParameterValue( desc.UseSnapshotIsolation );
                        }
                        else
                        {
                            monitor.Warn( $"Unable to find configuration for Database named '{db.Name}' of type '{db.GetType().FullName}'. Its ConnectionString will be null." );
                        }
                    }
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
        public SqlSetupAspect( SqlSetupAspectConfiguration config,
                               IActivityMonitor monitor )
        {
            Throw.CheckNotNullArgument( config );
            _config = config;
            _databases = new SqlManagerProvider( monitor, m => m.IgnoreMissingDependencyIsError = _config.IgnoreMissingDependencyIsError );
            _databases.Add( SqlDatabase.DefaultDatabaseName, _config.DefaultDatabaseConnectionString, autoCreate: true );
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


        bool IStObjEngineAspect.RunPreCode( IActivityMonitor monitor, IStObjEngineRunContext context )
        {
            return true;
        }

        bool IStObjEngineAspect.RunPostCode( IActivityMonitor monitor, IStObjEnginePostCodeRunContext context )
        {
            return true;
        }

        bool IStObjEngineAspect.Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context )
        {
            return true;
        }

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
        public void Dispose()
        {
            // Can safely be called multiple times.
            _databases.Dispose();
        }
    }
}
