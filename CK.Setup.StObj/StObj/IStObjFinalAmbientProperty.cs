using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Exposes an Ambient property that has not been resolved. It can be set by <see cref="IStObjValueResolver.ResolveExternalPropertyValue"/>.
    /// </summary>
    public interface IStObjFinalAmbientProperty : IStObjAmbientProperty
    {
        /// <summary>
        /// Gets the current value (<see cref="Type.Missing"/> as long as <see cref="SetValue"/> has not been called).
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Sets a value for this property.
        /// </summary>
        /// <param name="value">Value to set. Type must be compatible.</param>
        void SetValue( object value );

    }
}
