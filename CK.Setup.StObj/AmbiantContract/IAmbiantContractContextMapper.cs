using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface can be used to explicitely map types into typed context at the very beginning of
    /// the discovering/setup process. 
    /// Implementations of this interface MUST guaranty that no ambiguities can exist in a type inheritance chain:
    /// Whatever T1, T2 are if T1 and T2 are both mapped and T1 is an ancestor of T2, then T1 and T2 are mapped 
    /// to the same context (be it the null one).
    /// </summary>
    public interface IAmbiantContractContextMapper
    {
        /// <summary>
        /// Tries to map the type to a context.
        /// When returning false, <paramref name="context"/> is not touched in any way.
        /// When returning true, context can be null (null is used to designate the default context).
        /// </summary>
        /// <param name="t">The type to map.</param>
        /// <param name="context">Modified context if the type is mapped.</param>
        /// <returns>True if a mapping exists.</returns>
        bool FindExplicitContext( Type t, ref Type context );
    }
}
