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
        /// The <see cref="IResolvableReference.SetResolvedValue"/> can be used to set the parameter value.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="parameter">Parameter description.</param>
        void ResolveParameterValue( IActivityLogger logger, IParameter parameter );
        
        /// <summary>
        /// Dynamically called for each ambient property if automatic resolution failed to locate a StObj.
        /// The <see cref="IResolvableReference.SetResolvedValue"/> can be used to set the property value.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="ambientProperty">Property description.</param>
        void ResolveExternalPropertyValue( IActivityLogger logger, IAmbientProperty ambientProperty );
    }

}
