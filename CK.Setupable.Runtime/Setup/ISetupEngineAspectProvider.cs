using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Provides access to available <see cref="IStObjEngineAspect"/>.
    /// </summary>
    public interface ISetupEngineAspectProvider
    {
        /// <summary>
        /// Gets the <see cref="IStObjEngineAspect"/> that participate to setup.
        /// </summary>
        IReadOnlyList<IStObjEngineAspect> Aspects { get; }

        /// <summary>
        /// Gets the first <see cref="IStObjEngineAspect"/> that is assignable to <typeparamref name="T"/>. 
        /// If such aspect can not be found, depending on <paramref name="required"/> a <see cref="CKException"/> is thrown or null is returned.
        /// </summary>
        /// <typeparam name="T">Type of the aspect to obtain.</typeparam>
        /// <param name="required">False to silently return null instead of throwing an exception if the aspect can not be found.</param>
        /// <returns>The first compatible aspect (may be null if <param name="required"/> is false).</returns>
        T GetSetupEngineAspect<T>( bool required = true ) where T : class;
    }
}
