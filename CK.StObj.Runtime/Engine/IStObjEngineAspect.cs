using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Defines an aspect of the SetupEngine.
    /// <para>
    /// Concrete Aspect classes must implement this interface and have a public constructor
    /// that takes the configuration object instance.
    /// <see cref="Configure"/> will be called once all aspects have been instanciated.
    /// </para>
    /// <para>
    /// The configuration object is a <see cref="IStObjEngineAspectConfiguration"/> that has been 
    /// added to the <see cref="StObjEngineConfiguration.Aspects"/> list and 
    /// whose <see cref="IStObjEngineAspectConfiguration.AspectType"/> is the assembly qualified name
    /// of the Aspect they configure.
    /// </para>
    /// </summary>
    public interface IStObjEngineAspect
    {
        /// <summary>
        /// Called by the engine once all the aspects have been successfuly created, before actual Run.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="context">Configuration context.</param>
        /// <returns>
        /// Must return true on succes, false if any error occured (errors must be logged).
        /// Returning false stops the engine.
        /// </returns>
        bool Configure( IActivityMonitor monitor, IStObjEngineConfigurationContext context );

        bool OnStObjBuild( IActivityMonitor monitor, IStObjEngineStObjBuildContext context );
    }
}
