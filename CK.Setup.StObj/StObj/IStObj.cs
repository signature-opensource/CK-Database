using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A Structure Object "slices" an object (that is an <see cref="IAmbiantContract"/>) by 
    /// types in the inheritance chain.
    /// The <see cref="StObj">object</see> is built based on already built dependencies from top to bottom thanks to its "Construct" methods. 
    /// </summary>
    public interface IStObj
    {
        /// <summary>
        /// Gets the object.
        /// </summary>
        object StObj { get; }

        /// <summary>
        /// Gets the associated type (the "slice" of the object).
        /// </summary>
        Type ObjectType { get; }

        /// <summary>
        /// Gets whether this object has been referenced as a container by one or more structure objects.
        /// </summary>
        bool IsContainer { get; }

        /// <summary>
        /// Gets the parent Structure Object (the one associated to the base class of the object).
        /// May be null.
        /// </summary>
        IStObj Parent { get; }
        
    }
}
