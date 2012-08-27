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
        /// Gets or sets whether this reference must be satisfied with an available <see cref="IStObj"/> if the <see cref="P:Type"/> is not set to null.
        /// 
        /// Defaults to <see cref="StObjRequirementBehavior.ErrorIfNotStObj"/> for <see cref="IStObjMutableItem.Requires"/> and <see cref="IStObjMutableItem.Container"/> 
        /// (a described dependency is required unless explicitely declared as optional by <see cref="IStObjExternalConfigurator"/>).
        /// 
        /// Defaults to <see cref="StObjRequirementBehavior.None"/> for <see cref="IStObjMutableItem.Requiredby"/> since "required by" are always considered as optional.
        /// 
        /// Defaults to <see cref="StObjRequirementBehavior.WarnIfNotStObj"/> for Construct parameters since <see cref="IStObjDependencyResolver"/> can inject any dependency (the 
        /// dependency may even be missing - ie. null - if <see cref="IMutableParameterType.IsOptional"/> is true).
        /// </summary>
        StObjRequirementBehavior StObjRequirementBehavior { get; set; }

        /// <summary>
        /// Gets or sets the typed context associated to the <see cref="P:Type"/> of this reference.
        /// When not null, the type is searched in this typed context only. 
        /// When null, the type is first searched in the same typed context as this <see cref="Owner"/>.
        /// If not found, the type is searched in all context and, if it exists, it must exist in one and only one <see cref="IStObjContextualMapper"/>.
        /// </summary>
        Type Context { get; set; }

        /// <summary>
        /// Gets or sets the type of the reference. Can be set to null: container and requirements are ignored and 
        /// construct parameters are resolved to null (<see cref="IMutableParameterType.IsOptional"/> must be true).
        /// Of course, for construct parameters the type must be compatible with the formal parameter's type.
        /// </summary>
        Type Type { get; set; }
    }
}
