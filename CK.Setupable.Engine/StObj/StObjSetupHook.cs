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
            
            StObjCollectorResult result;
            using( monitor.OpenInfo().Send( "Handling StObj objects." ) )
            {
                AssemblyRegisterer typeReg = new AssemblyRegisterer( monitor );
                typeReg.TypeFilter = _config.TypeFilter;
                typeReg.Discover( _config.AppDomainConfiguration.Assemblies );
                StObjCollector stObjC = new StObjCollector( monitor, _runtimeBuilder, _configurator, _configurator, _configurator );
                using( monitor.OpenInfo().Send( "Registering StObj types." ) )
                {
                    stObjC.RegisterTypes( typeReg );
                    foreach( var t in _config.ExplicitRegisteredClasses ) stObjC.RegisterClass( t );
                    stObjC.DependencySorterHookInput = _center.StObjDependencySorterHookInput;
                    stObjC.DependencySorterHookOutput = _center.StObjDependencySorterHookOutput;
                    if( stObjC.RegisteringFatalOrErrorCount != 0 )
                    {
                        e.CancelSetup( "Error while registering StObj." );
                        return;
                    }
                }
                using( monitor.OpenInfo().Send( "Resolving StObj dependency graph." ) )
                {
                    result = stObjC.GetResult();
                    if( result.HasFatalError )
                    {
                        e.CancelSetup( "Error during StObj processing." );
                        return;
                    }
                }
                StObjContextRoot finalObjects;
                using( monitor.OpenInfo().Send( "Generating StObj dynamic assembly." ) )
                {
                    finalObjects = result.GenerateFinalAssembly( monitor, _runtimeBuilder, _config.FinalAssemblyConfiguration );
                    if( finalObjects == null )
                    {
                        e.CancelSetup( "Error during Final Assembly generation." );
                        return;
                    }
                }
                using( monitor.OpenInfo().Send( "Injecting final objects mapper." ) )
                {
                    if( !result.InjectFinalObjectAccessor( monitor, finalObjects ) )
                    {
                        e.CancelSetup( "Error while injecting final objects mapper." );
                        return;
                    }
                }
            }
            bool hasError = false;
            using( monitor.CatchCounter( a => hasError = true ) )
            {
                using( monitor.OpenInfo().Send( "Creating Setup Items from Structured Objects." ) )
                {
                    var itemBuilder = new StObjSetupItemBuilder( monitor, _configurator, _configurator, _configurator );
                    var setupItems = itemBuilder.Build( result.OrderedStObjs );
                    if( setupItems == null )
                    {
                        Debug.Assert( hasError );
                        monitor.CloseGroup( "Unable to create Setup items for StObjs." );
                    }
                    else e.Register( setupItems );
                }
            }
            if( hasError )
            {
                e.CancelSetup( "Error during Setup Items creation." );
                return;
            }
        }

    }
}
