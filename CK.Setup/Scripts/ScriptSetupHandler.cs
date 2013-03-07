using System;
using System.Diagnostics;
using CK.Core;
using System.Collections.Generic;

namespace CK.Setup
{
    public class ScriptSetupHandler : SetupHandler
    {
        readonly ScriptSet.ForHandler _scripts;

        public ScriptSetupHandler( SetupDriver driver, ScriptSet.ForHandler scripts )
            : base( driver )
        {
            if( scripts == null ) throw new ArgumentNullException( "scripts" );
            if( scripts.Count == 0 ) throw new ArgumentException( "No scripts provided.", "scripts" );
            _scripts = scripts;
        }

        bool Execute( SetupCallGroupStep step )
        {
            TypedScriptVector v = _scripts.GetScriptVector( step, Driver.ExternalVersion != null ? Driver.ExternalVersion.Version : null, Driver.ItemVersion );
            if( v == null || v.Scripts.Count == 0 ) return true;

            var logger = Driver.Engine.Logger;
            IScriptExecutor e = _scripts.Handler.CreateExecutor( logger, Driver );
            if( e == null )
            {
                logger.Error( "Unable to obtain a Script Executor for '{0}'.", _scripts.Handler.HandlerName );
                return false;
            }
            try
            {
                using( v.Scripts.Count > 1 ? logger.OpenGroup( LogLevel.Info, "Executing {1} '{0}' scripts.", _scripts.Handler.HandlerName, _scripts.Count ) : null )
                {
                    foreach( CoveringScript script in v.Scripts )
                    {
                        if( !e.ExecuteScript( logger, Driver, script.Script ) )
                        {
                            return false;
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                logger.Error( ex );
                return false;
            }
            finally
            {
                _scripts.Handler.ReleaseExecutor( logger, e );
            }
            return true;
        }

        protected override bool Init()
        {
            return Execute( SetupCallGroupStep.Init );
        }

        protected override bool InitContent()
        {
            return Execute( SetupCallGroupStep.InitContent );
        }

        protected override bool Install()
        {
            return Execute( SetupCallGroupStep.Install );
        }

        protected override bool InstallContent()
        {
            return Execute( SetupCallGroupStep.InstallContent );
        }

        protected override bool Settle()
        {
            return Execute( SetupCallGroupStep.Settle );
        }

        protected override bool SettleContent()
        {
            return Execute( SetupCallGroupStep.SettleContent );
        }

    }
}
