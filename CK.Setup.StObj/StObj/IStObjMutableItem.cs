using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.Setup
{
    public interface IStObjMutableItem
    {
        /// <summary>
        /// Gets the type of the structure object.
        /// </summary>
        Type ObjectType { get; }

        /// <summary>
        /// Gets the typed context where the structure object resides.
        /// </summary>
        Type Context { get; }

        /// <summary>
        /// Gets a mutable reference to the container of the object.
        /// Initialized by <see cref="StObjAttribute.Container"/> or any other <see cref="IStObjStructuralConfigurator"/>.
        /// When container is null and this StObj has a container of its Generalization will be used.
        /// </summary>
        IMutableReference Container { get; }

        /// <summary>
        /// Direct dependencies of the object.
        /// Initialized by <see cref="StObjAttribute.Requires"/>.
        /// </summary>
        IReadOnlyList<IMutableReference> Requires { get; }

        /// <summary>
        /// Reverse dependencies: types that depend on the object.
        /// Initialized by <see cref="StObjAttribute.RequiredBy"/>.
        /// </summary>
        IReadOnlyList<IMutableReference> RequiredBy { get; }

        /// <summary>
        /// Gets a list of mutable Construct parameters.
        /// </summary>
        IReadOnlyList<IMutableParameter> ConstructParameters { get; }

        /// <summary>
        /// Gets a list of mutable Ambient properties for the ultimate specialization: all
        /// ambient properties of the most specialized object are avalaible.
        /// This guarantees that properties are accessed by their most precise overriden/masked version.
        /// </summary>
        IReadOnlyList<IMutableAmbientProperty> AllAmbientProperties { get; }

        /// <summary>
        /// Sets a property on the actual object (the property must exist, its type must be compatible with the value 
        /// and be writeable - it must not be a non writeable mergeable property).
        /// Can be called for any writeable property of the object but when the property is an ambient one, this is
        /// the same as calling <see cref="IMutableAmbientProperty.SetStructuralValue"/>.
        /// </summary>
        /// <param name="logger">The logger to use to describe any error.</param>
        /// <param name="sourceName">The name of the "source" of this action.</param>
        /// <param name="propertyName">Name of the property that must exist.</param>
        /// <param name="value">Value to set.</param>
        /// <returns>True on success, false if any error occurs.</returns>
        bool SetPropertyStructuralValue( IActivityLogger logger, string sourceName, string propertyName, object value );
    }
}
