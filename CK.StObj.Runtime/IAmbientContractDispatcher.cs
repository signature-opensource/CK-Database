#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Runtime\IAmbientContractDispatcher.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface can be used to dynamically consider any Type as an Ambient contract and 
    /// explicitely dispatch types into type contexts at the very beginning of
    /// the discovering/setup process. 
    /// </summary>
    public interface IAmbientContractDispatcher
    {
        /// <summary>
        /// This method is called for any class Type that are not <see cref="AmbientContractCollector.IsStaticallyTypedAmbientContract">statically typed</see>
        /// as an ambient contract.
        /// As long as the implementation returns true for a Type, any specialization are automatically considered as an Ambient contract.
        /// </summary>
        /// <param name="t">Type that may be considered as an ambient contract.</param>
        /// <returns>Whether this type (and all its specializations) should be considered as an ambient contract.</returns>
        bool IsAmbientContractClass( Type t );

        /// <summary>
        /// Dispatchs the type to zero, one or mutiple contexts or keeps it in the programmatically defined contexts.
        /// Clearing <paramref name="contexts"/> removes the type from the whole setup.
        /// </summary>
        /// <param name="t">The type to map.</param>
        /// <param name="contexts">Context names into which the type is defined. This set can be changed.</param>
        void Dispatch( Type t, ISet<string> contexts );

    }
}
