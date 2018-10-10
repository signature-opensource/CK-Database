using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Exposes Service Types (interfaces and classes) to Service class mappings.
    /// </summary>
    public interface IStObjServiceMap
    {
        /// <summary>
        /// Gets all the <see cref="IAmbientService"/> types to the final service class type
        /// that can be directly resolved by any DI container.
        /// </summary>
        IReadOnlyDictionary<Type, IStObjServiceClassDescriptor> SimpleMappings { get; }

        /// <summary>
        /// Gets all the <see cref="IAmbientService"/> types to Service class mappings
        /// that cannot be directly resolved by a DI container and require either
        /// an adaptation based on the <see cref="IStObjServiceClassFactoryInfo"/> or
        /// to simply use the existing <see cref="IStObjServiceClassFactory.CreateInstance(IServiceProvider)"/>
        /// helper method.
        /// </summary>
        IReadOnlyDictionary<Type, IStObjServiceClassFactory> ManualMappings { get; }

        /// <summary>
        /// Gets the set of types that have been explicitly defined as singletons
        /// or inferred to be singletons.
        /// Note that this can contain open generic like <see cref="IPocoFactory{T}"/> (ie.
        /// the typeof(IPocoFactory&lt;&gt;) type) that is registered by default.
        /// </summary>
        IReadOnlyCollection<Type> ExternallyDefinedSingletons { get; }
    }
}
