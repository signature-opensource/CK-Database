#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\ScriptSetupHandlerBuilder.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Companion object of the <see cref="SetupEngine"/> that is automatically registered and manages
    /// the scripts: 
    /// - at <see cref="SetupStep.PreInit"/> it enables the object to load scripts into the <see cref="ScriptCollector"/> by 
    /// calling <see cref="SetupItemDriver.LoadScripts"/> on each driver
    /// - and at <see cref="SetupStep.Init"/> it associates a <see cref="ScriptSetupHandler"/> with the scripts that exist for each drivers.
    /// The scripts can come from the resources (the ones loaded by GenericItemSetupDriver.LoadScripts) or from the file system.
    /// </summary>
    class ScriptSetupHandlerBuilder
    {
        readonly SetupCoreEngine _engine;
        readonly ScriptCollector _scriptCollector;
        ScriptTypeManager _scriptManager;

        public ScriptSetupHandlerBuilder( SetupCoreEngine engine, ScriptCollector scripts, ScriptTypeManager scriptManager )
        {
            if( engine == null ) throw new ArgumentNullException( "center" );
            if( scripts == null ) throw new ArgumentNullException( "scripts" );
            if( scriptManager == null ) throw new ArgumentNullException( "scriptManager" );
            _engine = engine;
            _scriptCollector = scripts;
            _scriptManager = scriptManager;
            
            _engine.SetupEvent += OnSetupEvent;
        }

        void OnSetupEvent( object sender, SetupEventArgs e )
        {
            Debug.Assert( sender == _engine );
            if( e.ErrorOccurred ) 
            {
                // On any error, remove the event listener.
                _engine.DriverEvent -= OnDriverEvent;
                return;
            }
            if( e.Step == SetupStep.PreInit )
            {
                // At startup, subscribe to the Driver event.
                _engine.DriverEvent += OnDriverEvent;
            }
            else 
            {
                // For any step other than None, we do not need the event listener anymore.
                _engine.DriverEvent -= OnDriverEvent;

                // At initialization step, we create the ScriptSetupHandler for each registered scripts.
                if( e.Step == SetupStep.Init )
                {
                    var allHandlers = _scriptManager.GetSortedHandlers( _engine.Monitor );
                    if( allHandlers == null )
                    {
                        // GetSortedHandlers logged the detailed reason.
                        e.CancelSetup( "Errors while getting script handlers." );
                    }
                    if( allHandlers.Count > 0 )
                    {
                        foreach( var d in _engine.AllDrivers )
                        {
                            Debug.Assert( (d is SetupItemDriver) == !d.IsGroupHead, "There is only 2 DriverBase specializations: GenericItemSetupDriver and GroupHeadSetupDriver." );
                            SetupItemDriver driver = d as SetupItemDriver;
                            if( driver != null )
                            {
                                bool casingDiffer;
                                ScriptSet scripts = _scriptCollector.Find( driver.FullName, out casingDiffer );
                                if( scripts != null )
                                {
                                    if( casingDiffer )
                                    {
                                        e.CancelSetup( String.Format( "The names are case sensitive: setupable item '{0}' can not use scripts registered for '{1}'.", driver.FullName, scripts.FullName ) );
                                    }
                                    else
                                    {
                                        foreach( ScriptTypeHandler h in allHandlers )
                                        {
                                            ScriptSet.ForHandler fH = scripts.FindScripts( h );
                                            if( fH != null && fH.Count > 0 )
                                            {
                                                new ScriptSetupHandler( driver, fH );
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void OnDriverEvent( object sender, DriverEventArgs e )
        {
            Debug.Assert( sender == _engine );
            if( e.Step == SetupStep.PreInit && !e.Driver.IsGroupHead )
            {
                Debug.Assert( e.Driver is SetupItemDriver, "Since it is not the Head of a Group." );
                SetupItemDriver driver = (SetupItemDriver)e.Driver;
                if( !driver.LoadScripts( _scriptCollector ) )
                {
                    _engine.Monitor.Fatal().Send( "Driver '{0}' failed to load scripts.", e.Driver.FullName );
                    e.CancelSetup = true;
                }
           }
        }


    }
}
