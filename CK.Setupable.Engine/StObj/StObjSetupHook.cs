using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    class StObjSetupHook 
    {
        readonly SetupCenter _center;
        readonly SetupCenterConfiguration _config;
        readonly SetupableConfigurator _configurator;

        StObjCollectorResult _resultToGenerate;

        public StObjSetupHook( SetupCenter center, SetupCenterConfiguration config, SetupableConfigurator internalRelay )
        {
            _center = center;
            _config = config;
            _configurator = internalRelay;
            _center.RegisterSetupEvent += new EventHandler<RegisterSetupEventArgs>( center_RegisterSetupEvent );
            _center.SetupEvent += new EventHandler<SetupEventArgs>( _center_SetupEvent );
        }

        void center_RegisterSetupEvent( object sender, RegisterSetupEventArgs e )
        {
            var logger = _center.Logger;
            
            StObjCollectorResult result;
            using( logger.OpenGroup( LogLevel.Info, "Collecting objects." ) )
            {
                AssemblyRegisterer typeReg = new AssemblyRegisterer( logger );
                typeReg.TypeFilter = _config.TypeFilter;
                typeReg.Discover( _config.AssemblyRegistererConfiguration );
                StObjCollector stObjC = new StObjCollector( logger, _configurator, _configurator, _configurator );
                stObjC.RegisterTypes( typeReg );
                foreach( var t in _config.ExplicitRegisteredClasses ) stObjC.RegisterClass( t );
                stObjC.DependencySorterHookInput = _center.StObjDependencySorterHookInput;
                stObjC.DependencySorterHookOutput = _center.StObjDependencySorterHookOutput;
                if( stObjC.RegisteringFatalOrErrorCount != 0 )
                {
                    e.CancelSetup( "Error while registering StObj." );
                    return;
                }
                result = stObjC.GetResult();
                if( result.HasFatalError )
                {
                    e.CancelSetup( "Error during StObj processing." );
                    return;
                }
            }
            bool hasError = false;
            using( logger.CatchCounter( a => hasError = true ) )
            {
                using( logger.OpenGroup( LogLevel.Info, "Creating Setup Items from Structured Objects." ) )
                {
                    var itemBuilder = new StObjSetupItemBuilder( logger, _configurator, _configurator, _configurator );
                    e.Register( itemBuilder.Build( result.OrderedStObjs ) );
                }
            }
            if( hasError )
            {
                e.CancelSetup( "Error Setup Items creation." );
                return;
            }
            _resultToGenerate = result;
        }

        void _center_SetupEvent( object sender, SetupEventArgs e )
        {
            if( e.Step == SetupStep.Success )
            {
                _resultToGenerate.GenerateFinalAssembly( _center.Logger, _config.StObjFinalAssemblyConfiguration );
            }
        }

    }
}
