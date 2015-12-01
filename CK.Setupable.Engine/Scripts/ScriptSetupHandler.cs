#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\ScriptSetupHandler.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Diagnostics;
using CK.Core;
using System.Collections.Generic;

namespace CK.Setup
{
    public class ScriptSetupHandler : SetupHandler
    {
        readonly ScriptSet.ForHandler _scripts;

        public ScriptSetupHandler( GenericItemSetupDriver driver, ScriptSet.ForHandler scripts )
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

            var monitor = Driver.Engine.Monitor;
            IScriptExecutor e = _scripts.Handler.CreateExecutor( monitor, Driver );
            if( e == null )
            {
                monitor.Error().Send( "Unable to obtain a Script Executor for '{0}'.", _scripts.Handler.HandlerName );
                return false;
            }
            try
            {
                using( v.Scripts.Count > 1 ? monitor.OpenInfo().Send( "Executing {1} '{0}' scripts.", _scripts.Handler.HandlerName, _scripts.Count ) : null )
                {
                    foreach( CoveringScript script in v.Scripts )
                    {
                        if( !e.ExecuteScript( monitor, Driver, script.Script ) )
                        {
                            return false;
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex );
                return false;
            }
            finally
            {
                _scripts.Handler.ReleaseExecutor( monitor, e );
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
