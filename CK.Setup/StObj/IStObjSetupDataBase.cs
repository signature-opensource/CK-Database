using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface IStObjSetupDataBase
    {
        /// <summary>
        /// Gets the parent setup data if it exists (this is to manage attribute properties "inheritance"). 
        /// Null if this object corresponds to the first (root) <see cref="IAmbiantContract"/> of the inheritance chain.
        /// </summary>
        IStObjSetupData Parent { get; }

        /// <summary>
        /// Gets the associated <see cref="IStObj"/>.
        /// Never null.
        /// </summary>
        IStObj StObj { get; }

        /// <summary>
        /// Gets the [contextualized] full name of the object.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets whether the <see cref="FullName"/> is the default one (default full name is the <see cref="IStObj.ObjectType"/>.<see cref="Type.FullName">FullName</see>).
        /// </summary>
        bool IsDefaultFullName { get; }
    }
}
