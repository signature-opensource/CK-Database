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
        /// Dynamically called when a parameter value can not be automatically satisfied by an existing <see cref="IStObj"/>.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="parameter">Parameter description.</param>
        /// <returns>
        /// Any value (provided it is compatible with the expected type) or <see cref="Type.Missing"/> if no value can be resolved (this can be perfectly 
        /// valid depending on <see cref="IParameter.IsRealParameterOptional"/> and <see cref="IsOptional"/>).
        /// </returns>
        object ResolveParameterValue( IActivityLogger logger, IParameter parameter );
        
        /// <summary>
        /// Dynamically called when an ambiant property value can not be automatically satisfied by an existing <see cref="IStObj"/>.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="ambiantProperty">Property description.</param>
        /// <returns>
        /// Any value (provided it is compatible with the expected type) or <see cref="Type.Missing"/> if no value can be resolved (this can be perfectly 
        /// valid if <see cref="IAmbiantProperty.IsOptional"/> is true).
        /// </returns>
        object ResolvePropertyValue( IActivityLogger logger, IAmbiantProperty ambiantProperty );
    }

}
