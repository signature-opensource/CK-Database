using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Defines an aspect of the <see cref="SetupEngine"/>.
    /// 
    /// Concrete Aspect classes must implement this interface and have a public constructor
    /// that takes a SetupEngine and a configuration object instance. The constructor should only initializes its
    /// own objects and avoid interacting with the engine: <see cref="Configure"/>, that will be called once all 
    /// aspects have been instanciated, is the right place to initialize the engine.
    /// 
    /// The configuration object is a <see cref="ISetupEngineAspectConfiguration"/> that has been 
    /// added to the <see cref="SetupEngineConfiguration.Aspects"/> list and 
    /// whose <see cref="ISetupEngineAspectConfiguration.AspectType"/> is the assembly qualified name
    /// of the Aspect they configure.
    /// </summary>
    public interface ISetupEngineAspect
    {
        /// <summary>
        /// Gets the configuration object (from the constructor parameters).
        /// </summary>
        ISetupEngineAspectConfiguration Configuration { get; }

        /// <summary>
        /// Called by the engine once all the aspects have been successfuly created, before actual <see cref="SetupEngine.Run"/>.
        /// Aspects typically use and configure the <see cref="SetupEngine.StartConfiguration"/>.
        /// </summary>
        /// <returns>Must return true on succes, false if any error (that must have been logged) prevents the aspect to do its job.</returns>
        bool Configure();
    }
}
