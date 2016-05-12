#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\AutoImplementor\IDynamicAssembly.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections;
using System.Reflection.Emit;

namespace CK.Core
{
    /// <summary>
    /// Manages dynamic assembly creation with one <see cref="ModuleBuilder"/>.
    /// </summary>
    public interface IDynamicAssembly
    {
        /// <summary>
        /// Gets the <see cref="ModuleBuilder"/> for this <see cref="IDynamicAssembly"/>.
        /// </summary>
        ModuleBuilder ModuleBuilder { get; }

        /// <summary>
        /// Provides a new unique number that can be used for generating unique names inside this dynamic assembly.
        /// </summary>
        /// <returns>A unique number.</returns>
        string NextUniqueNumber();

        /// <summary>
        /// Gets a shared dictionary associated to the dynamic assembly. 
        /// Methods that generate code can rely on this to store shared information as required by their generation process.
        /// </summary>
        IDictionary Memory { get; }

        /// <summary>
        /// Pushes an action that will be executed before the generation of the final assembly: use this to 
        /// create final type from a <see cref="TypeBuilder"/> or to execute any action that must be done at the end 
        /// of the generation process.
        /// An action can be pushed at any moment: a pushed action can push another action.
        /// </summary>
        /// <param name="postAction">Action to execute.</param>
        void PushFinalAction( Action<IDynamicAssembly> postAction );
    }

}
