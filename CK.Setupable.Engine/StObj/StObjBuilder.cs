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
    class StObjBuilder 
    {
        static public IEnumerable<ISetupItem> SafeBuildStObj( SetupEngine engine, IStObjRuntimeBuilder runtimeBuilder, SetupEngineConfigurator configurator )
        {
            var monitor = engine.Monitor;
            var config = engine.Configuration.StObjEngineConfiguration;
            bool hasError = false;
            using( monitor.OnError( () => hasError = true ) )
            using( monitor.OpenInfo().Send( "Handling StObj objects." ) )
            {
                StObjCollectorResult result;
                AssemblyRegisterer typeReg = new AssemblyRegisterer( monitor );
                typeReg.Discover( config.BuildAndRegisterConfiguration.Assemblies );
                StObjCollector stObjC = new StObjCollector( monitor, config.TraceDependencySorterInput, config.TraceDependencySorterOutput, runtimeBuilder, configurator, configurator, configurator );
                stObjC.DependencySorterHookInput += engine.StartConfiguration.StObjDependencySorterHookInput;
                stObjC.DependencySorterHookOutput += engine.StartConfiguration.StObjDependencySorterHookOutput;
                using( monitor.OpenInfo().Send( "Registering StObj types." ) )
                {
                    stObjC.RegisterTypes( typeReg );
                    stObjC.RegisterClasses( config.BuildAndRegisterConfiguration.ExplicitClasses );
                    foreach( var t in engine.StartConfiguration.ExplicitRegisteredClasses ) stObjC.RegisterClass( t );
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
                        IEnumerable<ISetupItem> setupItems = null;
                        using( monitor.OpenInfo().Send( "Creating Setup Items from Structured Objects." ) )
                        {
                            var itemBuilder = new StObjSetupItemBuilder( monitor, engine.StartConfiguration.Aspects, configurator, configurator, configurator );
                            setupItems = itemBuilder.Build( result.OrderedStObjs );
                            Debug.Assert( setupItems != null || hasError, "setupItems == null ==> An error has been logged." );
                        }
                        if( setupItems != null )
                        {
                            StObjContextRoot finalObjects;
                            using( monitor.OpenInfo().Send( "Generating StObj dynamic assembly." ) )
                            {
                                finalObjects = result.GenerateFinalAssembly( monitor, runtimeBuilder, config.FinalAssemblyConfiguration );
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
                                    return setupItems;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
