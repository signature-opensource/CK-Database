using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface can be used to dynamically consider any Type as an Ambiant contract and 
    /// explicitely dispatch types into typed context at the very beginning of
    /// the discovering/setup process. 
    /// </summary>
    public interface IAmbiantContractDispatcher
    {
        /// <summary>
        /// Dispatchs the type to zero, one or mutiple contexts or keeps it in the programmatically defined contexts.
        /// Clearing <paramref name="contexts"/> removes the type from the whole setup.
        /// </summary>
        /// <param name="t">The type to map.</param>
        /// <param name="contexts">Contexts into which the type is defined. This set can be changed.</param>
        void Dispatch( Type t, ISet<Type> contexts );

        /// <summary>
        /// This method is called for any class Type that are not <see cref="AmbiantContractCollector.IsStaticallyTypedAmbiantContract">statically typed</see>
        /// as an ambiant contract.
        /// As long as the implmentation returns true for a Type, any specialization are automatically considered as an Ambiant contract.
        /// </summary>
        /// <param name="t">Type that may be considered as an ambiant contract.</param>
        /// <returns>Whether this type (and all its specializations) should be considered as an ambiant contract.</returns>
        bool IsAmbiantContractClass( Type t );
    }
}
