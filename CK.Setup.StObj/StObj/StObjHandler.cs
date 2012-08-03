using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{

    /// <summary>
    /// Defines a handler for typed object definition. Its responsibility is to 
    /// create one or more <see cref="IStObjDependentItem"/> given a type inheritance chain.
    /// </summary>
    /// <remarks>
    /// This handler class could have been designed as an interface or a delegate.
    /// </remarks>
    public abstract class StObjHandler
    {
        /// <summary>
        /// Attempts to register one or more <see cref="IStObjDependentItem"/> for <paramref name="pathTypes"/>.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="pathTypes">Types inheritance chain.</param>
        /// <param name="registerer">Registerer object to use.</param>
        /// <returns>
        /// True if <paramref name="pathTypes"/> has been handled by this handler. 
        /// False if subsequent handlers should be sollicitated.
        /// </returns>
        public abstract bool Register( IActivityLogger logger, IReadOnlyList<Type> pathTypes, IStObjRegisterer registerer );

    }
}
