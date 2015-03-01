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
        readonly SetupCenter _center;
        readonly IStObjRuntimeBuilder _runtimeBuilder;
        readonly SetupCenterConfiguration _config;
        readonly SetupableConfigurator _configurator;

        public StObjSetupHook( SetupCenter center, IStObjRuntimeBuilder runtimeBuilder, SetupCenterConfiguration config, SetupableConfigurator internalRelay )
        {
            _center = center;
            _runtimeBuilder = runtimeBuilder;
            _config = config;
            _configurator = internalRelay;
            _center.RegisterSetupEvent += new EventHandler<RegisterSetupEventArgs>( OnRegisterSetupEvent );
        }

        void OnRegisterSetupEvent( object sender, RegisterSetupEventArgs e )
        {
            var monitor = _center.Logger;

            bool hasError = false;
            using( monitor.CatchCounter( a => hasError = true ) )
            using( monitor.OpenInfo().Send( "Handling StObj objects." ) )
            {
                StObjCollectorResult result;
                AssemblyRegisterer typeReg = new AssemblyRegisterer( monitor );
                typeReg.TypeFilter = _config.TypeFilter;
                typeReg.Discover( _config.AppDomainConfiguration.Assemblies );
                //_config.FinalAssemblyConfiguration.ExternalVersionStamp;
                StObjCollector stObjC = new StObjCollector( monitor, _runtimeBuilder, _configurator, _configurator, _configurator );
                using( monitor.OpenInfo().Send( "Registering StObj types." ) )
                {
                    stObjC.RegisterTypes( typeReg );
                    foreach( var t in _config.ExplicitRegisteredClasses ) stObjC.RegisterClass( t );
                    stObjC.DependencySorterHookInput = _center.StObjDependencySorterHookInput;
                    stObjC.DependencySorterHookOutput = _center.StObjDependencySorterHookOutput;
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
