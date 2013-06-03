using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines an entry point that triggers the build of the system.
    /// This interface should be supported by an object with a public constructor that accepts
    /// a <see cref="IActivityLogger"/> and a <see cref="IStObjEngineConfiguration"/> (its assembly qualified name 
    /// must be specified as the <see cref="IStObjEngineConfiguration.BuilderAssemblyQualifiedName"/> property).
    /// </summary>
    public interface IStObjBuilder
    {
        /// <summary>
        /// Runs the full build of the system.
        /// </summary>
        /// <returns>True on success, false otherwise.</returns>
        bool Run();
    }
}
