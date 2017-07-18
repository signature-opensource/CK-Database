namespace CK.Core
{
    /// <summary>
    /// Provides minimal configuration required to produce a final (compiled) assembly.
    /// Thanks to this abstraction, <see cref="StObjContextRoot"/> is able to handle build/setup phases 
    /// that involve any higher level APIs than StObj itself.
    /// </summary>
    public interface IStObjBuilderConfiguration
    {
        /// <summary>
        /// Gets the Assembly Qualified Name of a <see cref="Type"/> that supports <see cref="IStObjBuilder"/>.
        /// It must have a public constructor that accepts an <see cref="IActivityMonitor"/>, an instance of 
        /// this <see cref="IStObjBuilderConfiguration"/> and a <see cref="IStObjRuntimeBuilder"/>.
        /// </summary>
        string BuilderAssemblyQualifiedName { get; }

        /// <summary>
        /// Gets the configuration related to the StObj: which assemblies and types 
        /// must be discovered, and configuration related to the emitted dll.
        /// </summary>
        StObjEngineConfiguration StObjEngineConfiguration { get; }

    }
}
