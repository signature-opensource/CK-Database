using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{

    public class ScriptSetupHandlerBuilder
    {
        readonly SetupEngine _engine;
        readonly ScriptCollector _scriptCollector;
        ScriptTypeManager _scriptManager;

        public ScriptSetupHandlerBuilder( SetupEngine engine, ScriptCollector scripts, ScriptTypeManager scriptManager )
        {
            if( engine == null ) throw new ArgumentNullException( "center" );
            if( scripts == null ) throw new ArgumentNullException( "scripts" );
            if( scriptManager == null ) throw new ArgumentNullException( "scriptManager" );
            _engine = engine;
            _scriptCollector = scripts;
            _scriptManager = scriptManager;
            _engine.DriverEvent += OnDriverEvent;
        }

        void OnDriverEvent( object sender, DriverEventArgs e )
        {
            Debug.Assert( sender == _engine );
            if( e.Step == SetupStep.None && !e.Driver.IsGroupHead )
            {
                Debug.Assert( e.Driver is SetupDriver, "Since it is not a Head." );

                bool casingDiffer;
                ScriptSet scripts = _scriptCollector.Find( e.Driver.FullName, out casingDiffer );
                if( scripts != null )
                {
                    if( casingDiffer )
                    {
                        _engine.Logger.Fatal( "The names are case sensitive: setupable item '{0}' can not use scripts registered for '{1}'.", e.Driver.FullName, scripts.FullName );
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
                        else if( allHandlers.Count > 0 )
                        {
                            var d = (SetupDriver)e.Driver;
                            foreach( ScriptTypeHandler h in allHandlers )
                            {
                                ScriptSet.ForHandler fH = scripts.FindScripts( h );
                                if( fH != null && fH.Count > 0 )
                                {
                                    new ScriptSetupHandler( d, fH );
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
