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

        ///// <summary>
        ///// Gets the root Structure Object (the one associated to the first class marked 
        ///// as a <see cref="IAmbiantContract"/> in the inheritance chain).
        ///// May be this object itself.
        ///// </summary>
        //IStObj Root { get; }
        
        ///// <summary>
        ///// Gets the most precise Structure Object (the one associated to the type of the <see cref="StObj"/> itself.
        ///// May be this object itself.
        ///// </summary>
        //IStObj Leaf { get; }

    }
}
