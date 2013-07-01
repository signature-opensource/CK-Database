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
    /// mainly a demonstrator of the minimal code required to analyse dependencies and build a StObj final assembly.
    /// </summary>
    public class BasicStObjBuilder : IStObjBuilder
    {
        readonly IActivityLogger _logger;
        readonly IStObjEngineConfiguration _config;

        /// <summary>
        /// Initializes a new <see cref="BasicStObjBuilder"/>.
        /// Its assembly qualified name ("CK.Setup.BasicStObjBuilder, CK.StObj.Engine") can be set as the <see cref="IStObjEngineConfiguration.BuilderAssemblyQualifiedName"/>
        /// for minimal build (simple objects and no dynamic configuration).
        /// </summary>
        /// <param name="logger">Logger that must be used.</param>
        /// <param name="config">Configuration that descrives the key aspects of the build.</param>
        public BasicStObjBuilder( IActivityLogger logger, IStObjEngineConfiguration config )
        {
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Builds the object graph.
        /// </summary>
        /// <returns>True on success, false if an error occured.</returns>
        public bool Run()
        {
            // Step 1: Dicovering assemblies from AssemblyRegisterConfiguration.
            AssemblyRegisterer typeReg = new AssemblyRegisterer( _logger );
            typeReg.Discover( _config.AppDomainConfiguration.Assemblies );

            // Step 2: Collecting StObj (AmbientContracts) from assemblies.
            StObjCollector collector = new StObjCollector( _logger );
            collector.RegisterTypes( typeReg );
            if( collector.RegisteringFatalOrErrorCount > 0 ) return false;

            // Step 3: Resolving dependencies and building ordered graph.
            var r = collector.GetResult();
            if( r.HasFatalError ) return false;

            // Step 4: Generating final assembly.
            return r.GenerateFinalAssembly( _logger, _config.FinalAssemblyConfiguration );
        }
    }
}
