using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Defines a named reference to an item: this interface must be supported by items that really are named references
    /// to items (such as <see cref="NamedDependentItemRef"/>).
    /// </summary>
    public interface IDependentItemNamedRef : IDependentItemRef 
    {
        /// <summary>
        /// Makes sure that this <see cref="IDependentItemRef.FullName"/> has a [context] prefix (and returns this instance) or
        /// creates a new <see cref="IDependentItemNamedRef"/> with the given prefix.
        /// </summary>
        /// <param name="defaultContextName">Context name to inject if no context prefix exists.</param>
        /// <returns>This instance or a new prefixed one.</returns>
        IDependentItemNamedRef DoEnsureContextPrefix( string defaultContextName );
    
    }
}
