using System;

namespace CK.Setup
{

    /// <summary>
    /// Describes a reference to a type.
    /// It may be optional and/or targets a specific typed <see cref="P:Context"/>.
    /// </summary>
    public interface IMutableReferenceType
    {
        /// <summary>
        /// Gets the item that owns this reference.
        /// </summary>
        IStObjMutableItem Owner { get; }

        /// <summary>
        /// Gets the kind of reference.
        /// </summary>
        MutableReferenceKind Kind { get; }

        /// <summary>
        /// Gets or sets whether this reference must be satisfied with an available <see cref="IStObj"/>.
        /// Defaults to true for <see cref="IStObjMutableItem.Requires"/> and <see cref="IStObjMutableItem.Container"/> (a described dependency or container is 
        /// required unless explicitely declared as optional) 
        /// and false for <see cref="IStObjMutableItem.Requiredby"/> and <see cref="IStObjMutableItem.ConstructParameters"/> since for Construct parameters any 
        /// injected dependency can be used (the dependency may even be missing - ie. stays unresolved - if <see cref="IMutableParameterType.IsOptional"/> is true).
        /// </summary>
        bool StObjRequired { get; set; }

        /// <summary>
        /// Gets or sets the typed context associated to the <see cref="P:Type"/> of this reference.
        /// When not null, the type is searched in this typed context only. 
        /// When null, the type is first searched in the same typed context as this <see cref="Owner"/>.
        /// If not found, the type is searched in all context and, if it exists, it must exist in one and only one <see cref="IStObjContextualMapper"/>.
        /// </summary>
        Type Context { get; set; }

        /// <summary>
        /// Gets or sets the type of the reference.
        /// </summary>
        Type Type { get; set; }
    }
}
