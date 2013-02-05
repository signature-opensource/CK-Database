using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Core
{
    /// <summary>
    /// Provides minimal configuration required to produce 
    /// a final (compiled) assembly. Thanks to this abstraction, <see cref="StObjContextRoot"/>
    /// </summary>
    public interface IStObjEngineConfiguration
    {
        /// <summary>
        /// Gets the Assembly Qualified Name of a <see cref="Type"/> that must have a public
        /// constructor that accepts an <see cref="IActivityLogger"/> and an instance of 
        /// this <see cref="IStObjEngineConfiguration"/>. It must support <see cref="IStObjBuilder"/>.
        /// </summary>
        string BuilderAssemblyQualifiedName { get; }

        /// <summary>
        /// Gets the configuration related to the app domain is the setup phasis.
        /// </summary>
        StObjBuilderAppDomainConfiguration StObjBuilderAppDomainConfiguration { get; }

        /// <summary>
        /// Gets the configuration related to final assembly generation.
        /// </summary>
        StObjFinalAssemblyConfiguration StObjFinalAssemblyConfiguration { get; }

    }
}
