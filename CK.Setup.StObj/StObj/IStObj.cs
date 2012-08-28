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
        /// Gets the typed context where the structure object resides.
        /// </summary>
        Type Context { get; }

        /// <summary>
        /// Gets whether this object has been referenced as a container by one or more structure objects.
        /// </summary>
        bool IsContainer { get; }

        /// <summary>
        /// Gets the parent Structure Object in the inheritance chain (the one associated to the base class of the object).
        /// May be null.
        /// </summary>
        IStObj Generalization { get; }

        /// <summary>
        /// Gets the child Structure Object in the inheritance chain.
        /// May be null.
        /// </summary>
        IStObj Specialization { get; }

        /// <summary>
        /// Gets this object and its children Structure Objects down to the most specialized one.
        /// May be empty.
        /// </summary>
        IEnumerable<IStObj> SpecializationPath { get; }

        /// <summary>
        /// Gets the container object. 
        /// May be null.
        /// </summary>
        IStObj Container { get; }

        /// <summary>
        /// Gets a list of required objects. 
        /// </summary>
        IReadOnlyList<IStObj> Requires { get; }
        
        /// <summary>
        /// Gets a list of objects that require this object. 
        /// </summary>
        IReadOnlyList<IStObj> RequiredBy { get; }
    }
}
