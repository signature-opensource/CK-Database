using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Exposes a resolvable reference (can be a <see cref="IAmbientProperty"/> or a <see cref="IParameter"/>).
    /// <see cref="SetResolvedValue"/> can be used to set the <see cref="Value"/> to use.
    /// </summary>
    public interface IResolvableReference
    {
        /// <summary>
        /// Gets the item that exposes this reference.
        /// </summary>
        IStObj Owner { get; }
        
        /// <summary>
        /// Gets the typed context associated to the <see cref="P:Type"/> of this reference.
        /// </summary>
        Type Context { get; }

        /// <summary>
        /// Gets the expected type of the reference value.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the name of the ambient property or parameter.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether this reference can be considered as optional. When true, <see cref="Value"/> can be <see cref="Type.Missing"/>:
        /// if automatic resolution fails then, for a property it is simply not set and, for a parameter, behavior depends on <see cref="IParameter.IsRealParameterOptional"/>.
        /// </summary>
        bool IsOptional { get; }

        /// <summary>
        /// Gets the current value that will be used. If it has not been "structurally" set by one <see cref="IStObjStructuralConfigurator.Configurator"/>,
        /// it is <see cref="Type.Missing"/>. 
        /// Use <see cref="SetResolvedValue"/> to set it.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Sets the <see cref="Value"/>.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="value">
        /// Any value (provided it is compatible with the expected <see cref="P:Type"/>) or <see cref="Type.Missing"/> to let the
        /// automatic resolution does its job.
        /// </param>
        /// <returns>True on success, false if any error occured (errors are logged).</returns>
        bool SetResolvedValue( IActivityLogger logger, object value );


    }
}
