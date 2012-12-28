using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
namespace CK.Setup
{
    /// <summary>
    /// Describes a parameter of a Construct method.
    /// </summary>
    public interface IStObjMutableParameter : IStObjMutableReference
    {
        /// <summary>
        /// Gets the name of the construct parameter.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets whether the resolution of this parameter can be considered as optional.
        /// When changed by <see cref="IStObjStructuralConfigurator"/> from false to true (see remarks) and the resolution fails, the default 
        /// value of the parameter or the default value for the parameter's type is automatically used (null for reference types).
        /// </summary>
        /// <remarks>
        /// If this is originally false, it means that the the formal parameter of the method is NOT optional (<see cref="IsRealParameterOptional"/> is false). 
        /// </remarks>
        bool IsOptional { get; set; }

        /// <summary>
        /// Gets the parameter position in the list.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets whether the formal parameter is optional (<see cref="Type.Missing"/> can be used as the parameter value 
        /// at invocation time, see <see cref="ParameterInfo.IsOptional"/>).
        /// </summary>
        bool IsRealParameterOptional { get; }

        /// <summary>
        /// Sets the value for this parameter.
        /// By setting an explicit value through this method, the <see cref="IStObjMutableReference.Context"/> and <see cref="IStObjMutableReference.Type"/> that describes
        /// a reference to a <see cref="IStObj"/> are ignored: this breaks the potential dependency to the <see cref="IAmbientContact"/> object that may be referenced.
        /// </summary>
        /// <remarks>
        /// The <see cref="IStObjFinalParameter"/> also exposes this method: by using <see cref="IStObjFinalParameter.SetParameterValue"/> from <see cref="IStObjValueResolver.ResolveParameterValue"/>, an 
        /// explicit value can be injected while the potential dependency has actually been taken into account.
        /// </remarks>
        /// <param name="value">Value to set. Type must be compatible otherwise an exception will be thrown when calling the actual Construct method.</param>
        void SetParameterValue( object value );
    }
}
