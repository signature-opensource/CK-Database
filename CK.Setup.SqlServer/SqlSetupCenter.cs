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
        //TypedObjectCollector _collector;      

        public SqlSetupCenter( SqlSetupContext context )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            _context = context;
            var versionRepo = new SqlVersionedItemRepository( _context.DefaultDatabase );
            var memory = new SqlSetupSessionMemoryProvider( _context.DefaultDatabase );
            _center = new SetupCenter( versionRepo, memory,_context.Logger, _context );
            _fileDiscoverer = new SqlFileDiscoverer( new SqlObjectBuilder(), _context.Logger );
            //_collector = new TypedObjectCollector( context.TypedObjectHandlers.ToReadOnlyList() );
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
        

        /// <summary>
        /// Explicitely discovers objects and types in the given assembly (its dependencies are not processed).
        /// This should be used if and only if <see cref="SqlSetupContext.AutomaticAssemblyDiscovering"/> is set to false.
        /// </summary>
        /// <param name="assembly">The assembly to discover.</param>
        public void ExplicitDiscover( Assembly assembly )
        {
//            _collector.RegisterTypes( assembly, _context.Logger ); 
        }


        /// <summary>
        /// Executes the setup. See remarks.
        /// </summary>
        /// <returns>True if no error occured. False otherwise.</returns>
        public bool Run()
        {
            return _center.Run( _fileDiscoverer );

            //if( _context.AutomaticAssemblyDiscovering )
            //{
            //    using( _context.Logger.OpenGroup( LogLevel.Info, "Automatic discovering of currently loaded assemblies." ) )
            //    {
            //        AssemblyDiscoverer p = new AssemblyDiscoverer( _context.Logger );
            //        try
            //        {
            //            p.Start( a => _context.IgnoredAssemblies.Contains( a.GetName().Name ) == false, ExplicitDiscover );
            //            p.DiscoverCurrenlyLoadedAssemblies();
            //        }
            //        finally
            //        {
            //            p.Stop();
            //        }
            //    }
            //}
            //MultiTypedObjectResult r = _collector.GetResult( _context.Logger );
            //return !r.HasFatalError && _center.Run( _fileDiscoverer, r );
        }
    }
}
