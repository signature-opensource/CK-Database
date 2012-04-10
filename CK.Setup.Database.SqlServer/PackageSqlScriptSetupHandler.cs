using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.Database.SqlServer
{
    class PackageSqlScriptSetupHandler : SetupHandlerContainer
    {
        readonly IDatabaseExecutor _db;
        readonly PackageScriptSet _scripts;

        public PackageSqlScriptSetupHandler( SetupDriverContainer driver, PackageScriptSet scripts, IDatabaseExecutor db )
            : base( driver )
        {
            if( scripts == null ) throw new ArgumentNullException( "scripts" );
            if( db == null ) throw new ArgumentNullException( "db" );
            _db = db;
            _scripts = scripts;
        }

        bool Execute( SetupCallContainerStep step )
        {
            PackageScriptVector v = _scripts.GetScriptVector( "sql", step, Driver.ExternalVersion != null ? Driver.ExternalVersion.Version : null, Driver.Item.Version );
            foreach( var s in v.Scripts )
            {
                if( !_db.ExecuteScript( s.GetScript() ) ) return false;
            }
            return true;
        }

        protected override bool Init()
        {
            return Execute( SetupCallContainerStep.Init );
        }

        protected override bool InitContent()
        {
            return Execute( SetupCallContainerStep.InitContent );
        }

        protected override bool Install()
        {
            return Execute( SetupCallContainerStep.Install );
        }

        protected override bool InstallContent()
        {
            return Execute( SetupCallContainerStep.InstallContent );
        }

        protected override bool Settle()
        {
            return Execute( SetupCallContainerStep.Settle );
        }

        protected override bool SettleContent()
        {
            return Execute( SetupCallContainerStep.SettleContent );
        }

    }
}
