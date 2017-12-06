using CK.CodeGen;
using CK.CodeGen.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CK.Core
{
    /// <summary>
    /// Supports assembly generation.
    /// Actual support is not required by the model layer: runtime and engine are in charge of
    /// extending this abstraction in any required way.
    /// </summary>
    public interface IDynamicAssembly
    {
        /// <summary>
        /// Provides a new unique number that can be used for generating unique names inside this dynamic assembly.
        /// </summary>
        /// <returns>A unique number.</returns>
        string NextUniqueNumber();

        /// <summary>
        /// Gets a shared dictionary associated to the dynamic assembly. 
        /// Methods that generate code can rely on this to store shared information as required by their generation process.
        /// </summary>
        IDictionary Memory { get; }

        /// <summary>
        /// Gets the <see cref="ModuleBuilder"/> for this <see cref="IDynamicAssembly"/>.
        /// </summary>
        ModuleBuilder ModuleBuilder { get; }

        /// <summary>
        /// Gets the default name space for this <see cref="IDynamicAssembly"/>
        /// into which code should be generated.
        /// Note that nothing prevents the <see cref="ICodeScope.Workspace"/> to be used and other
        /// namespaces to be created.
        /// </summary>
        INamespaceScope DefaultGenerationNamespace { get; }

        /// <summary>
        /// Gets a mutable list of source code generator modules for this <see cref="IDynamicAssembly"/>.
        /// </summary>
        IList<ICodeGeneratorModule> SourceModules { get; }

    }

}
