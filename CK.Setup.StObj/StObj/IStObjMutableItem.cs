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
        /// Gets or sets the container of the object.
        /// Initialized by <see cref="StObjAttribute.Container"/>.
        /// </summary>
        IMutableReferenceType Container { get; }

        /// <summary>
        /// Direct dependencies of the object.
        /// Initialized by <see cref="StObjAttribute.Requires"/>.
        /// </summary>
        IReadOnlyList<IMutableReferenceType> Requires { get; }

        /// <summary>
        /// Reverse dependencies: types that depend on the object.
        /// Initialized by <see cref="StObjAttribute.RequiredBy"/>.
        /// </summary>
        IReadOnlyList<IMutableReferenceType> RequiredBy { get; }

        /// <summary>
        /// Gets a list of mutable Construct parameters.
        /// </summary>
        IReadOnlyList<IMutableParameterType> ConstructParameters { get; }

    }
}
