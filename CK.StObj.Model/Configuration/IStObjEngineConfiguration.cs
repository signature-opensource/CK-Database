using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Core
{
    /// <summary>
    /// Provides minimal configuration required to produce a final (compiled) assembly.
    /// Thanks to this abstraction, <see cref="StObjContextRoot"/> is able to handle build/setup phasis 
    /// that involve any higher level APIs than StObj itself.
    /// </summary>
    public interface IStObjEngineConfiguration
    {
        /// <summary>
        /// Gets the Assembly Qualified Name of a <see cref="Type"/> that must have a public
        /// constructor that accepts an <see cref="IActivityLogger"/> and an instance of 
        /// this <see cref="IStObjEngineConfiguration"/>.
        /// It must support <see cref="IStObjBuilder"/>.
        /// </summary>
        string BuilderAssemblyQualifiedName { get; }

        /// <summary>
        /// Gets the configuration that describes how Application Domain must be used during build.
        /// </summary>
        BuilderAppDomainConfiguration AppDomainConfiguration { get; }

        /// <summary>
        /// Gets the configuration related to final assembly generation.
        /// </summary>
        BuilderFinalAssemblyConfiguration FinalAssemblyConfiguration { get; }

    }
}
