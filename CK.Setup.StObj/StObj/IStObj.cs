using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A StObj "slices" a Structured Object (that is an <see cref="IAmbiantContract"/>) by 
    /// types in its inheritance chain.
    /// The <see cref="StructuredObject">Structured Object</see> is built based on already built dependencies from top to bottom thanks to its "Construct" methods. 
    /// </summary>
    public interface IStObj : IStructuredObjectHolder
    {
        /// <summary>
        /// Gets the associated type (the "slice" of the object).
        /// </summary>
        Type ObjectType { get; }

        /// <summary>
        /// Gets the typed context where the structure object resides.
        /// </summary>
        Type Context { get; }

        /// <summary>
        /// Gets whether this object has been referenced as a container by one or more structure objects.
        /// </summary>
        bool IsContainer { get; }

        /// <summary>
        /// Gets the parent <see cref="IStObj"/> in the inheritance chain (the one associated to the base class of this <see cref="ObjectType"/>).
        /// May be null.
        /// </summary>
        IStObj Generalization { get; }

        /// <summary>
        /// Gets the child <see cref="IStObj"/> in the inheritance chain.
        /// May be null.
        /// </summary>
        IStObj Specialization { get; }

        /// <summary>
        /// Gets the ultimate generalization <see cref="IStObj"/> in the inheritance chain. Never null (can be this object itself).
        /// </summary>
        IStObj RootGeneralization { get; }

        /// <summary>
        /// Gets the ultimate specialization <see cref="IStObj"/> in the inheritance chain. Never null (can be this object itself).
        /// </summary>
        IStObj LeafSpecialization { get; }

        /// <summary>
        /// Gets the configured container for this object. If this <see cref="Container"/> has been inherited 
        /// from its <see cref="Generalization"/>, this ConfiguredContainer is null.
        /// </summary>
        IStObj ConfiguredContainer { get; }

        /// <summary>
        /// Gets the container of this object. If no container has been explicitely associated for the object, this is the
        /// container of its <see cref="Generalization"/> (if it exists). May be null.
        /// </summary>
        IStObj Container { get; }

        /// <summary>
        /// Gets a list of required objects. 
        /// </summary>
        IReadOnlyList<IStObj> Requires { get; }
        
    }
}
