using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A StObj "slices" a Structured Object (that is an <see cref="IAmbientContract"/>) by 
    /// types in its inheritance chain.
    /// The <see cref="StructuredObject">Structured Object</see> itself is built based on already built dependencies from top to bottom thanks to its "Construct" methods. 
    /// </summary>
    public interface IStObj : IStructuredObjectHolder
    {
        /// <summary>
        /// Gets the associated type (the "slice" of the object).
        /// </summary>
        Type ObjectType { get; }

        /// <summary>
        /// Gets the context name where the structure object resides.
        /// </summary>
        string Context { get; }

        /// <summary>
        /// Gets kind of structure object for this StObj. It can be a <see cref="DependentItemKind.Item"/>, 
        /// a <see cref="DependentItemKind.Group"/> or a <see cref="DependentItemKind.Container"/>.
        /// </summary>
        DependentItemKind ItemKind { get; }

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
        /// Gets a list of required objects. This list combines the requirements of this items (explicitely required types, 
        /// construct parameters, etc.) and any RequiredBy from other objects.
        /// </summary>
        IReadOnlyList<IStObj> Requires { get; }

        /// <summary>
        /// Gets a list of Group objects to which this object belongs.
        /// </summary>
        IReadOnlyList<IStObj> Groups { get; }

        /// <summary>
        /// Gets a list of children objects when this <see cref="ItemKind"/> is either a <see cref="DependentItemKind.Group"/> or a <see cref="DependentItemKind.Container"/>.
        /// </summary>
        IReadOnlyList<IStObj> Children { get; }

        /// <summary>
        /// Gets the list of Ambient Properties that reference this object.
        /// </summary>
        IReadOnlyList<IStObjTrackedAmbientPropertyInfo> TrackedAmbientProperties { get; }

        /// <summary>
        /// Gets the value of the named property that may be associated to this StObj or to any StObj 
        /// in <see cref="Container"/> or <see cref="Generalization"/> 's chains (recursively).
        /// </summary>
        /// <param name="propertyName">Name of the property. Must not be null nor empty.</param>
        /// <returns>The property value (can be null) if the property has been defined, <see cref="Type.Missing"/> otherwise.</returns>
        object GetStObjProperty( string propertyName );
        
    }
}
