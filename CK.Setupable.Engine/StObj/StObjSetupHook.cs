#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\StObj\StObjSetupHook.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    class StObjSetupHook 
    {
        readonly SetupEngine _engine;
        readonly IStObjRuntimeBuilder _runtimeBuilder;
        readonly StObjEngineConfiguration _config;
        readonly SetupEngineConfigurator _configurator;

        public StObjSetupHook( SetupEngine engine, IStObjRuntimeBuilder runtimeBuilder, StObjEngineConfiguration config, SetupEngineConfigurator internalRelay )
        {
            _engine = engine;
            _runtimeBuilder = runtimeBuilder;
            _config = config;
            _configurator = internalRelay;
            _engine.RegisterSetupEvent += new EventHandler<RegisterSetupEventArgs>( OnRegisterSetupEvent );
        }

        void OnRegisterSetupEvent( object sender, RegisterSetupEventArgs e )
        {
            var monitor = _engine.Monitor;

            bool hasError = false;
            using( monitor.OnError( () => hasError = true ) )
            using( monitor.OpenInfo().Send( "Handling StObj objects." ) )
            {
                StObjCollectorResult result;
                AssemblyRegisterer typeReg = new AssemblyRegisterer( monitor );
                typeReg.Discover( _config.BuildAndRegisterConfiguration.Assemblies );
                StObjCollector stObjC = new StObjCollector( monitor, _config.TraceDependencySorterInput, _config.TraceDependencySorterOutput, _runtimeBuilder, _configurator, _configurator, _configurator );
                stObjC.DependencySorterHookInput += _engine.StartConfiguration.StObjDependencySorterHookInput;
                stObjC.DependencySorterHookOutput += _engine.StartConfiguration.StObjDependencySorterHookOutput;
                using( monitor.OpenInfo().Send( "Registering StObj types." ) )
                {
                    stObjC.RegisterTypes( typeReg );
                    stObjC.RegisterClasses( _config.BuildAndRegisterConfiguration.ExplicitClasses );
                    foreach( var t in _engine.StartConfiguration.ExplicitRegisteredClasses ) stObjC.RegisterClass( t );
                    Debug.Assert( stObjC.RegisteringFatalOrErrorCount == 0 || hasError, "stObjC.RegisteringFatalOrErrorCount > 0 ==> An error has been logged." );
                }
                if( stObjC.RegisteringFatalOrErrorCount == 0 )
                {
                    using( monitor.OpenInfo().Send( "Resolving StObj dependency graph." ) )
                    {
                        result = stObjC.GetResult();
                        Debug.Assert( !result.HasFatalError || hasError, "result.HasFatalError ==> An error has been logged." );
                    }
                    if( !result.HasFatalError )
                    {
                        IEnumerable<ISetupItem> setupItems;
                        using( monitor.OpenInfo().Send( "Creating Setup Items from Structured Objects." ) )
                        {
                            var itemBuilder = new StObjSetupItemBuilder( monitor, _configurator, _configurator, _configurator );
                            setupItems = itemBuilder.Build( result.OrderedStObjs );
                            Debug.Assert( setupItems != null || hasError, "setupItems == null ==> An error has been logged." );
                        }
                        if( setupItems != null )
                        {
                            StObjContextRoot finalObjects;
                            using( monitor.OpenInfo().Send( "Generating StObj dynamic assembly." ) )
                            {
                                finalObjects = result.GenerateFinalAssembly( monitor, _runtimeBuilder, _config.FinalAssemblyConfiguration );
                                Debug.Assert( finalObjects != null || hasError, "finalObjects == null ==> An error has been logged." );
                            }
                            if( finalObjects != null )
                            {
                                bool injectDone;
                                using( monitor.OpenInfo().Send( "Injecting final objects mapper." ) )
                                {
                                    injectDone = result.InjectFinalObjectAccessor( monitor, finalObjects );
                                    Debug.Assert( injectDone || hasError, "inject failed ==> An error has been logged." );
                                }
                                if( injectDone )
                                {
                                    e.Register( setupItems );
                                }
                            }
                        }
                    }
                }
            }
            if( hasError )
            {
                e.CancelSetup( "Error during Setup Items creation." );
            }
        }

    }
}
