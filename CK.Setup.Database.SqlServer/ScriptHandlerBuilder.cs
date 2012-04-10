using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup.Database.SqlServer
{
    public class ScriptHandlerBuilder
    {
        readonly SetupCenter _center;
        readonly PackageScriptCollector _scripts;

        // TEMPORARY BIDOUILLE INFÂME
        readonly IDatabaseExecutor _db;


        public ScriptHandlerBuilder( SetupCenter center, PackageScriptCollector scripts, /*TEMPORARY BIDOUILLE INFÂME!!!*/IDatabaseExecutor db )
        {
            if( center == null ) throw new ArgumentNullException( "center" );
            if( scripts == null ) throw new ArgumentNullException( "scripts" );
            if( db == null ) throw new ArgumentNullException( "db" );
            _center = center;
            _scripts = scripts;
            _db = db;
            _center.DriverEvent += OnDriverEvent;
        }

        void OnDriverEvent( object sender, SetupDriverEventArgs e )
        {
            Debug.Assert( sender == _center );
            if( e.Step == SetupStep.None && !e.Driver.IsContainerHead && e.Driver is SetupDriverContainer )
            {
                bool casingDiffer;
                PackageScriptSet scripts = _scripts.Find( e.Driver.FullName, out casingDiffer );
                if( scripts != null )
                {
                    if( casingDiffer )
                    {
                        _center.Logger.Warn( "The names are case sensitive: setupable item {0} can not use scripts registered for {1}.", e.Driver.FullName, scripts.PackageFullName );
                    }
                    else
                    {
                        if( scripts.ForType( "sql" ).Any() )
                        {
                            new PackageSqlScriptSetupHandler( (SetupDriverContainer)e.Driver, scripts, _db );
                        }
                    }
                }
            }
        }

    }
}
