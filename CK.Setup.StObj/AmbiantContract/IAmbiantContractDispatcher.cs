using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface can be used to explicitely dispatch types into typed context at the very beginning of
    /// the discovering/setup process. 
    /// </summary>
    public interface IAmbiantContractDispatcher
    {
        /// <summary>
        /// Dispatchs the type to zero, one or mutiple contexts or keeps it in the programmatically defined contexts.
        /// </summary>
        /// <param name="t">The type to map.</param>
        /// <param name="contexts">Contexts into which the type must be defined.</param>
        void Dispatch( Type t, ISet<Type> contexts );
    }
}
