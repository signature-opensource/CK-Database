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
    public class SqlSetupCenter
    {
        SqlSetupContext _context;
        SetupCenter _center;
        SqlFileDiscoverer _fileDiscoverer;
        AmbiantContractCollector _collector;

        public SqlSetupCenter( SqlSetupContext context )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            _context = context;
            var versionRepo = new SqlVersionedItemRepository( _context.DefaultDatabase );
            var memory = new SqlSetupSessionMemoryProvider( _context.DefaultDatabase );
            _center = new SetupCenter( versionRepo, memory,_context.Logger, _context );
            _fileDiscoverer = new SqlFileDiscoverer( new SqlObjectBuilder(), _context.Logger );
            _collector = new AmbiantContractCollector();
            _center.ScriptTypeManager.Register( new SqlScriptTypeHandler( _context.DefaultDatabase ) );
        }

        public bool DiscoverFilePackages( string directoryPath )
        {
            return _fileDiscoverer.DiscoverPackages( directoryPath );
        }

        public bool DiscoverSqlFiles( string directoryPath )
        {
            return _fileDiscoverer.DiscoverSqlFiles( directoryPath, _center.Scripts );
        }

        public void DiscoverObjects( Assembly assembly )
        {
            _collector.Register( assembly.GetTypes() ); 
        }

        public bool Run()
        {
            var r = _collector.GetResult();
            if( !r.CheckErrorAndWarnings( _context.Logger ) ) return false;


            return _center.Run( _fileDiscoverer );
        }
    }
}
