using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Enables explicit configuration of Construct method parameters as well as manual resolution for ambient 
    /// properties that are not bound to <see cref="IStObj"/> objects. 
    /// Must be passed as a parameter to the constructor of <see cref="StObjCollector"/>.
    /// </summary>
    public interface IStObjValueResolver
    {
        /// <summary>
        /// Dynamically called for each parameter before invoking the construct method.
        /// The <see cref="IStObjFinalParameter.SetParameterValue"/> can be used to set or alter the parameter value.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="parameter">Parameter of a Construct method.</param>
        void ResolveParameterValue( IActivityLogger logger, IStObjFinalParameter parameter );
        
        /// <summary>
        /// Dynamically called for each ambient property onky if automatic resolution failed to locate a StObj.
        /// The <see cref="IStObjFinalAmbientProperty.SetValue"/> can be used to set the property value.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="ambientProperty">Property for which a value should be set.</param>
        void ResolveExternalPropertyValue( IActivityLogger logger, IStObjFinalAmbientProperty ambientProperty );
    }

}
