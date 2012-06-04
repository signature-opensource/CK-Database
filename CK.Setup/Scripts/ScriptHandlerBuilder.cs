using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{

    public class ScriptHandlerBuilder
    {
        readonly SetupEngine _engine;
        readonly PackageScriptCollector _scriptCollector;
        ScriptTypeManager _scriptManager;

        public ScriptHandlerBuilder( SetupEngine engine, PackageScriptCollector scripts, ScriptTypeManager scriptManager )
        {
            if( engine == null ) throw new ArgumentNullException( "center" );
            if( scripts == null ) throw new ArgumentNullException( "scripts" );
            if( scriptManager == null ) throw new ArgumentNullException( "scriptManager" );
            _engine = engine;
            _scriptCollector = scripts;
            _scriptManager = scriptManager;
            _engine.DriverEvent += OnDriverEvent;
        }

        void OnDriverEvent( object sender, SetupDriverEventArgs e )
        {
            Debug.Assert( sender == _engine );
            if( e.Step == SetupStep.None && !e.Driver.IsContainerHead && e.Driver is SetupDriverContainer )
            {
                bool casingDiffer;
                PackageScriptSet scripts = _scriptCollector.Find( e.Driver.FullName, out casingDiffer );
                if( scripts != null )
                {
                    if( casingDiffer )
                    {
                        _engine.Logger.Fatal( "The names are case sensitive: setupable item '{0}' can not use scripts registered for '{1}'.", e.Driver.FullName, scripts.PackageFullName );
                        e.CancelSetup = true;
                    }
                    else
                    {
                        var allHandlers = _scriptManager.GetSortedHandlers( _engine.Logger );
                        if( allHandlers == null )
                        {
                            // GetSortedHandlers already logged the reason.
                            e.CancelSetup = true;
                        }
                        else
                        {
                            var scriptTypes = scripts.ScriptTypes.ToReadOnlyCollection();
                            var scriptHandlers = scriptTypes.Join( allHandlers, Util.FuncIdentity, h => h.ScriptType, ( type, handler ) => handler ).ToReadOnlyList();

                            if( scriptTypes.Count != scriptHandlers.Count )
                            {
                                _engine.Logger.Fatal( "Missing Script Type Handlers for types: '{0}'", String.Join( "', '", scriptTypes.Except( scriptHandlers.Select( h => h.ScriptType ) ) ) );
                                e.CancelSetup = true;
                            }
                            else if( scriptHandlers.Count > 0 )
                            {
                                var d = (SetupDriverContainer)e.Driver;
                                new PackageScriptSetupHandler( d, scripts, scriptHandlers );
                            }
                        }
                    }
                }
            }
        }

    }
}
