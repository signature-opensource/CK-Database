using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Extends the descriptive <see cref="IStObjServiceClassFactoryInfo"/> with a concrete
    /// implementation of a factory method based on an external <see cref="IServiceProvider"/>.
    /// </summary>
    public interface IStObjServiceClassFactory : IStObjServiceClassFactoryInfo
    {
        /// <summary>
        /// Actual object factory.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        object CreateInstance( IServiceProvider provider );
    }
}
