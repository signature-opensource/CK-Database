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
        /// <summary>
        /// Exposes the <see cref="OrderedStObjs"/> and the resulting <see cref="SetupItems"/> for them
        /// and captures required context (<see cref="StObjCollectorResult"/>, <see cref="IStObjRuntimeBuilder"/> and <see cref="BuilderFinalAssemblyConfiguration"/>)
        /// to be able to <see cref="GenerateFinalAssembly"/>.
        /// </summary>
        public class BuildStObjResult
        {
            readonly StObjCollectorResult _result;
            readonly IStObjRuntimeBuilder _runtimeBuilder;
            readonly BuilderFinalAssemblyConfiguration _configuration;

            public BuildStObjResult( StObjCollectorResult r, IEnumerable<ISetupItem> items, IStObjRuntimeBuilder runtimeBuilder, BuilderFinalAssemblyConfiguration configuration )
            {
                _result = r;
                SetupItems = items;
                _runtimeBuilder = runtimeBuilder;
                _configuration = configuration;
            }

            /// <summary>
            /// Gets the ordered <see cref="IStObjResult"/> list.
            /// </summary>
            public readonly IReadOnlyList<IStObjResult> OrderedStObjs;

            /// <summary>
            /// Gets all the <see cref="ISetupItem"/> for the StObj.
            /// </summary>
            public readonly IEnumerable<ISetupItem> SetupItems;

            /// <summary>
            /// Generates the final assembly.
            /// </summary>
            /// <param name="monitor">Monitor to use.</param>
            /// <param name="injectFinalObjectAccessor">True to set the <see cref="IStObjResult.ObjectAccessor"/> to return the real final object.</param>
            /// <returns>True on success, false on error.</returns>
            public bool GenerateFinalAssembly( IActivityMonitor monitor, bool injectFinalObjectAccessor = false )
            {
                bool hasError = false;
                using( monitor.OnError( () => hasError = true ) )
                {
                    StObjContextRoot finalObjects;
                    using( monitor.OpenInfo().Send( "Generating StObj dynamic assembly." ) )
                    {
                        finalObjects = _result.GenerateFinalAssembly( monitor, _runtimeBuilder, _configuration );
                        Debug.Assert( finalObjects != null || hasError, "finalObjects == null ==> An error has been logged." );
                    }
                    if( finalObjects != null )
                    {
                        bool injectDone;
                        using( monitor.OpenInfo().Send( "Injecting final objects mapper." ) )
                        {
                            injectDone = _result.InjectFinalObjectAccessor( monitor, finalObjects );
                            Debug.Assert( injectDone || hasError, "inject failed ==> An error has been logged." );
                        }
                        return injectDone;
                    }
                    return false;
                }
            }
        }

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
                            bool hasError2 = false;
                            using( monitor.OnError( () => hasError2 = true ) )
                            {
                                StObjContextRoot finalObjects;
                                using( monitor.OpenInfo().Send( "Generating StObj dynamic assembly." ) )
                                {
                                    finalObjects = result.GenerateFinalAssembly( monitor, runtimeBuilder, config.FinalAssemblyConfiguration );
                                    Debug.Assert( finalObjects != null || hasError2, "finalObjects == null ==> An error has been logged." );
                                }
                                if( finalObjects != null )
                                {
                                    bool injectDone;
                                    using( monitor.OpenInfo().Send( "Injecting final objects mapper." ) )
                                    {
                                        injectDone = result.InjectFinalObjectAccessor( monitor, finalObjects );
                                        Debug.Assert( injectDone || hasError2, "inject failed ==> An error has been logged." );
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
            }
            return null;
        }
    }
}
