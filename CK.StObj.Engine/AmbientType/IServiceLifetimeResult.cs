using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Gives access to the services lifetime analysis result.
    /// </summary>
    public interface IServiceLifetimeResult
    {
        /// <summary>
        /// Gets whether a type has been defined or inferred (and must be) a singleton.
        /// </summary>
        bool IsExternalSingleton( Type t );

        /// <summary>
        /// Gets the set of types that have been explicitly defined as singletons
        /// or inferred to be singletons.
        /// Note that this can contain open generic like <see cref="IPocoFactory{T}"/> (ie.
        /// the typeof(IPocoFactory&lt;&gt;) type).
        /// </summary>
        IReadOnlyCollection<Type> ExternallyDefinedSingletons { get; }
    }
}
