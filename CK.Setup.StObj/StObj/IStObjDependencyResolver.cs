using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// 
    /// </summary>
    public interface IStObjDependencyResolver
    {
        /// <summary>
        /// Dynamically called for each parameter before automatic resolution.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="parameter">
        /// Parameter description with a mutable <see cref="IResolvableReference.Value"/>.
        /// </param>
        void ResolveParameterValue( IActivityLogger logger, IParameter parameter );
        
        /// <summary>
        /// Dynamically called for each parameter before automatic resolution.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="ambiantProperty">
        /// Property description with a mutable <see cref="IResolvableReference.Value"/>.
        /// </param>
        void ResolvePropertyValue( IActivityLogger logger, IAmbiantProperty ambiantProperty );
    }

}
