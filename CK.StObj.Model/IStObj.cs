using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public interface IStObj
    {
        /// <summary>
        /// Gets the associated type (the "slice" of the object).
        /// </summary>
        Type ObjectType { get; }

        /// <summary>
        /// Gets the context where the structure object resides.
        /// </summary>
        IContextualStObjMap Context { get; }

        /// <summary>
        /// Gets the parent <see cref="IStObj"/> in the inheritance chain (the one associated to the base class of this <see cref="ObjectType"/>).
        /// May be null.
        /// </summary>
        IStObj Generalization { get; }

        /// <summary>
        /// Gets the child <see cref="IStObj"/> in the inheritance chain.
        /// May be null.
        /// </summary>
        IStObj Specialization { get; }
    }
}
