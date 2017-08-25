using System;
using System.Collections.Generic;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class SetupableAspect : IStObjEngineAspect
    {
        readonly SetupableAspectConfiguration _config;
        IVersionedItemReader _versionedItemReader;
        IVersionedItemWriter _versionedItemWriter;
        ISetupSessionMemoryProvider _setupSessionMemoryProvider;

        public SetupableAspect( SetupableAspectConfiguration config )
        {
            _config = config;
        }

        public bool Configure( IActivityMonitor monitor, IStObjEngineConfigurationContext context )
        {

            context.PushPostConfigureAction( PostConfigure );
            return true;
        }

        bool PostConfigure( IActivityMonitor monitor, IStObjEngineConfigurationContext context )
        {
            _versionedItemReader = context.ServiceContainer.GetService<IVersionedItemReader>( true );
            _versionedItemWriter = context.ServiceContainer.GetService<IVersionedItemWriter>( true );
            _setupSessionMemoryProvider = context.ServiceContainer.GetService<ISetupSessionMemoryProvider>( true );
            return true;
        }

        public bool OnStObjBuild( IActivityMonitor monitor, IStObjEngineStObjBuildContext context )
        {
            var path = monitor.Output.RegisterClient( new ActivityMonitorPathCatcher() );
            ISetupSessionMemory m = null;
            try
            {
                var itemBuilder = new StObjSetupItemBuilder( monitor, engine.StartConfiguration.Aspects, configurator, configurator, configurator );
                IEnumerable<ISetupItem> setupItems = itemBuilder.Build( context.OrderedStObjs );
                if( setupItems == null ) return false;
                m = _setupSessionMemoryProvider.StartSetup();
                VersionedItemTracker versionTracker = new VersionedItemTracker( _versionedItemReader );
                if( versionTracker.Initialize( monitor ) )
                {
                    bool setupSuccess = DoRun( items, buildResult.SetupItems, versionTracker, m );
                    setupSuccess &= versionTracker.ConcludeWithFatalOnError( monitor, _versionedItemWriter, setupSuccess );
                    return setupSuccess;
                }
            }
            catch( Exception ex )
            {
                monitor.Fatal().Send( ex );
            }
            finally
            {
                monitor.Output.UnregisterClient( path );
            }
            if( m != null ) _setupSessionMemoryProvider.StopSetup( path.LastErrorPath.ToStringPath() );
            return false;
        }
    }
}
