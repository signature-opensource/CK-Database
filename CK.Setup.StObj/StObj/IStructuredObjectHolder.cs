using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Minimal abstraction for any object that carries a (or gives access to) Structured Object.
    /// </summary>
    /// <remarks>
    /// The <see cref="IStObj"/> interface that are built by <see cref="StObjCollector"/> are IStructuredObjectHolder
    /// and other objects (that encapsulate <see cref="IStObj"/> for instance) can also use this interface.
    /// </remarks>
    public interface IStructuredObjectHolder
    {
        /// <summary>
        /// Gets the associated object instance (the final, most specialized, structured object).
        /// </summary>
        object Object { get; }

    }
}
