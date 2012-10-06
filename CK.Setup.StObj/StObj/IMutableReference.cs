using System;

namespace CK.Setup
{
    /// <summary>
    /// Describes a reference to a type.
    /// It may be optional and/or targets a specific typed <see cref="P:Context"/>.
    /// </summary>
    public interface IMutableReference
    {
        /// <summary>
        /// Gets the item that owns this reference. See remarks.
        /// </summary>
        /// <remarks>
        /// Owner of the reference corresponds to the exact type of the object that has the Construct method for parameters.
        /// For Ambient Properties, the Owner is the Specialization. See remarks.
        /// </remarks>
        /// <remarks>
        /// For Ambient Properties, this is because a property has de facto more than one Owner when masking is used: spotting one of them requires to 
        /// choose among them - The more abstract one? The less abstract one? - and this would be both ambiguate and quite useless since in practice, 
        /// the "best Owner" must be based on the actual property type to take Property Covariance into account.
        /// </remarks>
        IStObjMutableItem Owner { get; }

        /// <summary>
        /// Gets the kind of reference.
        /// </summary>
        MutableReferenceKind Kind { get; }

        /// <summary>
        /// Gets or sets whether this reference must be satisfied with an available <see cref="IStObj"/> if the <see cref="P:Type"/> is not set to null.
        /// <para>
        /// Defaults to <see cref="StObjRequirementBehavior.ErrorIfNotStObj"/> for <see cref="IStObjMutableItem.Requires"/> and <see cref="IStObjMutableItem.Container"/> 
        /// (a described dependency is required unless explicitely declared as optional by <see cref="IStObjStructuralConfigurator"/>).
        /// </para>
        /// <para>
        /// Defaults to <see cref="StObjRequirementBehavior.WarnIfNotStObj"/> for Construct parameters since <see cref="IStObjDependencyResolver"/> can inject any dependency (the 
        /// dependency may even be missing - ie. let to null for refernce types - if <see cref="IMutableParameter.IsOptional"/> is true).
        /// </para>
        /// <para>
        /// Defaults to <see cref="StObjRequirementBehavior.None"/> for ambient properties and <see cref="IStObjMutableItem.Requiredby"/> since "required by" are always considered as optional.
        /// </para>
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
        /// construct parameters are resolved to their default (<see cref="IMutableParameter.IsOptional"/> must be true).
        /// Of course, for construct parameters the type must be compatible with the formal parameter's type (similar
        /// type compatibility is required for ambient properties).
        /// </summary>
        /// <remarks>
        /// Initialized with the <see cref="System.Reflection.PropertyInfo.PropertyType"/> for Ambient Properties, 
        /// with <see cref="System.Reflection.ParameterInfo.ParameterType"/> for parameters and with provided type 
        /// for other kind of reference (<see cref="MutableReferenceKind.Requires"/>, <see cref="MutableReferenceKind.RequiredBy"/> and <see cref="MutableReferenceKind.Container"/>).
        /// </remarks>
        Type Type { get; set; }

    }
}
