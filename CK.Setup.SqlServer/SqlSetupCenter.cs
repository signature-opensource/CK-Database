using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.SqlServer;
using CK.Setup.Database;
using System.Reflection;

namespace CK.Setup.SqlServer
{
    public class SqlSetupCenter : ISetupDriverFactory
    {
        SqlSetupContext _context;
        SetupCenter _center;
        SqlFileDiscoverer _fileDiscoverer;
        DependentProtoItemCollector _sqlFiles;

        public SqlSetupCenter( SqlSetupContext context )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            _context = context;
            var versionRepo = new SqlVersionedItemRepository( _context.DefaultSqlDatabase );
            var memory = new SqlSetupSessionMemoryProvider( _context.DefaultSqlDatabase );
            _center = new SetupCenter( versionRepo, memory,_context.Logger, this );
            _sqlFiles = new DependentProtoItemCollector();
            _fileDiscoverer = new SqlFileDiscoverer( new SqlObjectParser(), _context.Logger );
            
            var sqlHandler = new SqlScriptTypeHandler( _context );
            // Registers source "res-sql" first: resource scripts have low priority.
            sqlHandler.RegisterSource( "res-sql" );
            // Then registers "file-sql".
            sqlHandler.RegisterSource( SqlFileDiscoverer.DefaultSourceName );

            _center.ScriptTypeManager.Register( sqlHandler );
        }

        /// <summary>
        /// Gets ors sets whether the ordering for setupable items that share the same rank in the pure dependency graph must be inverted.
        /// Defaults to false. (See <see cref="DependencySorter"/> for more information.)
        /// </summary>
        public bool RevertOrderingNames 
        {
            get { return _center.RevertOrderingNames; } 
            set { _center.RevertOrderingNames = value; }
        }

        public bool DiscoverFilePackages( string directoryPath )
        {
            return _fileDiscoverer.DiscoverPackages( String.Empty, SqlDefaultDatabase.DefaultDatabaseName, directoryPath );
        }

        public bool DiscoverSqlFiles( string directoryPath )
        {
            return _fileDiscoverer.DiscoverSqlFiles( String.Empty, SqlDefaultDatabase.DefaultDatabaseName, directoryPath, _sqlFiles, _center.Scripts );
        }
        
        /// <summary>
        /// Executes the setup.
        /// </summary>
        /// <returns>True if no error occured. False otherwise.</returns>
        public bool Run()
        {
            var logger = _context.Logger;

            StObjCollectorResult result;
            using( logger.OpenGroup( LogLevel.Info, "Collecting objects." ) )
            {
                AssemblyRegisterer typeReg = new AssemblyRegisterer( logger );
                typeReg.Discover( _context.AssemblyRegistererConfiguration );
                StObjCollector stObjC = new StObjCollector( logger, _context.StObjConfigurator, _context.StObjConfigurator, _context.StObjConfigurator );
                stObjC.RegisterTypes( typeReg );
                foreach( var t in _context.ExplicitRegisteredClasses ) stObjC.RegisterClass( t );
                result = stObjC.GetResult();
                if( result.HasFatalError ) return false;
            }
            
            IEnumerable<IDependentItem> stObjItems;
            using( logger.OpenGroup( LogLevel.Info, "Creating Dependent Items from Structured Objects." ) )
            {
                var itemBuilder = new StObjSetupBuilder( logger, _context.StObjConfigurator );
                stObjItems = itemBuilder.Build( result.OrderedStObjs );
            }
            IEnumerable<IDependentItem> sqlObjectsFromFiles;
            using( logger.OpenGroup( LogLevel.Info, "Creating Sql Objects from {0} sql files.", _sqlFiles.Count ) )
            {
                sqlObjectsFromFiles = _sqlFiles.OfType<SqlObjectProtoItem>().Select( proto => proto.CreateItem( logger ) );
            }
            return _center.Run( sqlObjectsFromFiles, _fileDiscoverer.DiscoveredPackages, stObjItems );
        }

        SetupDriver ISetupDriverFactory.CreateDriver( Type driverType, SetupDriver.BuildInfo info )
        {
            if( driverType == typeof( SqlObjectSetupDriver ) ) return new SqlObjectSetupDriver( info, _context );
            if( driverType == typeof( SetupDriver ) ) return new SetupDriver( info );
            if( driverType == typeof( SqlDatabaseSetupDriver ) ) return new SqlDatabaseSetupDriver( info );
            if( driverType == typeof( SqlDatabaseConnectionSetupDriver ) ) return new SqlDatabaseConnectionSetupDriver( info, _context );
            if( driverType == typeof( SqlPackageSetupDriver ) ) return new SqlPackageSetupDriver( info );
            if( driverType == typeof( SqlTableSetupDriver ) ) return new SqlTableSetupDriver( info );
            return null;
        }


    }
}
