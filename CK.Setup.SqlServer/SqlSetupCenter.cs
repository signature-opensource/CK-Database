using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.SqlServer;
using CK.Setup.Database;

namespace CK.Setup.SqlServer
{
    public class SqlSetupCenter
    {
        SqlSetupContext _context;
        SetupCenter _center;
        SqlFileDiscoverer _discoverer;

        public SqlSetupCenter( SqlSetupContext context )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            _context = context;
            var versionRepo = new SqlVersionedItemRepository( _context.DefaultDatabase );
            var memory = new SqlSetupSessionMemoryProvider( _context.DefaultDatabase );
            _center = new SetupCenter( versionRepo, memory,_context.Logger, _context );
            _discoverer = new SqlFileDiscoverer( new SqlObjectBuilder(), _context.Logger );
            _center.ScriptTypeManager.Register( new SqlScriptTypeHandler( _context.DefaultDatabase ) );
        }

        public bool DiscoverFilePackages( string directoryPath )
        {
            return _discoverer.DiscoverPackages( directoryPath );
        }

        public bool DiscoverSqlFiles( string directoryPath )
        {
            return _discoverer.DiscoverSqlFiles( directoryPath, _center.Scripts );
        }

        public bool Run()
        {
            return _center.Run( _discoverer );
        }

    }
}
