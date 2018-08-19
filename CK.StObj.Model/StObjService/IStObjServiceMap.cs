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
        /// Gets all the <see cref="IAmbientService"/> types to Service class mappings
        /// that can be directly resolved by any DI container.
        /// </summary>
        IReadOnlyDictionary<Type, Type> SimpleMappings { get; }

        /// <summary>
        /// Gets all the <see cref="IAmbientService"/> types to Service class mappings
        /// that cannot be directly resolved by a DI container and require either
        /// an adaptation based on the <see cref="IStObjServiceClassFactoryInfo"/> or
        /// to simply use the existing <see cref="IStObjServiceClassFactory.CreateInstance(IServiceProvider)"/>
        /// helper method.
        /// </summary>
        IReadOnlyDictionary<Type, IStObjServiceClassFactory> ManualMappings { get; }
    }
}
