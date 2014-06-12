using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.Setup
{
    /// <summary>
    /// Basic builder works with mere <see cref="IStObjEngineConfiguration"/>.
    /// There is no configuration involved here since this would require (at the Model layer) interfaces defined in Runtime layer, this is
    /// mainly a demonstrator of the minimal code required to analyze dependencies and build a StObj final assembly.
    /// </summary>
    public class BasicStObjBuilder : IStObjBuilder
    {
        readonly IActivityMonitor _monitor;
        readonly IStObjEngineConfiguration _config;
        readonly IStObjRuntimeBuilder _runtimeBuilder;

        /// <summary>
        /// Initializes a new <see cref="BasicStObjBuilder"/>.
        /// Its assembly qualified name ("CK.Setup.BasicStObjBuilder, CK.StObj.Engine") can be set as the <see cref="IStObjEngineConfiguration.BuilderAssemblyQualifiedName"/>
        /// for minimal build (simple objects and no dynamic configuration).
        /// </summary>
        /// <param name="monitor">Logger that must be used.</param>
        /// <param name="config">Configuration that describes the key aspects of the build.</param>
        /// <param name="runtimeBuilder">The object in charge of actual objects instanciation. When null, <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/> is used.</param>
        public BasicStObjBuilder( IActivityMonitor monitor, IStObjEngineConfiguration config, IStObjRuntimeBuilder runtimeBuilder = null )
        {
            _monitor = monitor;
            _config = config;
            _runtimeBuilder = runtimeBuilder ?? StObjContextRoot.DefaultStObjRuntimeBuilder;
        }

        /// <summary>
        /// Builds the object graph.
        /// </summary>
        /// <returns>True on success, false if an error occurred.</returns>
        public bool Run()
        {
            // Step 1: Discovering assemblies from AssemblyRegisterConfiguration.
            AssemblyRegisterer typeReg = new AssemblyRegisterer( _monitor );
            typeReg.Discover( _config.AppDomainConfiguration.Assemblies );

            // Step 2: Collecting StObj (AmbientContracts) from assemblies.
            StObjCollector collector = new StObjCollector( _monitor );
            collector.RegisterTypes( typeReg );
            if( collector.RegisteringFatalOrErrorCount > 0 ) return false;

            // Step 3: Resolving dependencies and building ordered graph.
            var r = collector.GetResult();
            if( r.HasFatalError ) return false;

            // Step 4: Generating final assembly.
            return r.GenerateFinalAssembly( _monitor, _runtimeBuilder, _config.FinalAssemblyConfiguration ) != null;
        }
    }
}
