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
        /// Gets the context where the structure object resides.
        /// </summary>
        string Context { get; }

        /// <summary>
        /// Gets the kind of object (simple item, group or container).
        /// </summary>
        DependentItemKind ItemKind { get; set; }

        /// <summary>
        /// Gets or sets how Ambient Properties that reference this StObj must be considered.
        /// </summary
        TrackAmbientPropertiesMode TrackAmbientProperties { get; set; }

        /// <summary>
        /// Gets a mutable reference to the container of the object.
        /// Initialized by <see cref="StObjAttribute.Container"/> or any other <see cref="IStObjStructuralConfigurator"/>.
        /// When the configured container's type is null and this StObj has a Generalization, the container of its Generalization will be used.
        /// </summary>
        IStObjMutableReference Container { get; }

        /// <summary>
        /// Contained items of the object.
        /// Initialized by <see cref="StObjAttribute.Children"/>.
        /// </summary>
        IStObjMutableReferenceList Children { get; }

        /// <summary>
        /// Direct dependencies of the object.
        /// Initialized by <see cref="StObjAttribute.Requires"/>.
        /// </summary>
        IStObjMutableReferenceList Requires { get; }

        /// <summary>
        /// Reverse dependencies: types that depend on the object.
        /// Initialized by <see cref="StObjAttribute.RequiredBy"/>.
        /// </summary>
        IStObjMutableReferenceList RequiredBy { get; }

        /// <summary>
        /// Reverse dependencies: types that depend on the object.
        /// Initialized by <see cref="StObjAttribute.RequiredBy"/>.
        /// </summary>
        IStObjMutableReferenceList Groups { get; }

        /// <summary>
        /// Gets a list of mutable Construct parameters.
        /// </summary>
        IReadOnlyList<IStObjMutableParameter> ConstructParameters { get; }

        /// <summary>
        /// Gets a list of Ambient properties defined at this level (and above) but potentially specialized.
        /// This guarantees that properties are accessed by their most precise overriden/masked version.
        /// To explicitely set a value for an ambient property or alter its configuration, use <see cref="SetAmbiantPropertyValue"/>
        /// or <see cref="SetAmbiantPropertyConfiguration"/>.
        /// </summary>
        IReadOnlyList<IStObjAmbientProperty> SpecializedAmbientProperties { get; }

        /// <summary>
        /// Gets a list of mutable <see cref="IStObjMutableAmbientContract"/> defined at this level (and above) but potentially specialized.
        /// This guarantees that properties are accessed by their most precise overriden/masked version.
        /// </summary>
        IReadOnlyList<IStObjMutableAmbientContract> SpecializedAmbientContracts { get; }

        /// <summary>
        /// Sets a direct property (it must not be an Ambient Property, Contract nor a StObj property) on the Structured Object. 
        /// The property must exist, be writeable and the type of the <paramref name="value"/> must be compatible with the property type otherwise an error is logged.
        /// </summary>
        /// <param name="logger">The logger to use to describe any error.</param>
        /// <param name="propertyName">Name of the property to set.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="sourceDescription">Optional description of the origin of the value to help troubleshooting.</param>
        /// <returns>True on success, false if any error occurs.</returns>
        bool SetDirectPropertyValue( IActivityLogger logger, string propertyName, object value, string sourceDescription = null );

        /// <summary>
        /// Sets a property on the StObj. The property must not be an ambient property, but it is not required to be 
        /// defined by a <see cref="StObjPropertyAttribute"/> (see remarks).
        /// </summary>
        /// <remarks>
        /// A StObj property can be dynamically defined on any StObj. The StObjPropertyAttribute enables definition and Type restriction 
        /// of StObj properties by the holding type itself, but is not required.
        /// </remarks>
        /// <param name="logger">The logger to use to describe any error.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="sourceDescription">Optional description of the origin of the value to help troubleshooting.</param>
        /// <returns>True on success, false if any error occurs.</returns>
        bool SetStObjPropertyValue( IActivityLogger logger, string propertyName, object value, string sourceDescription = null );

        /// <summary>
        /// Sets an ambient property on the Structured Object (the property must exist, be writeable, and marked with <see cref="AmbientPropertyAttribute"/>). The
        /// type of the <paramref name="value"/> must be compatible with the property type otherwise an error is logged.
        /// </summary>
        /// <param name="logger">The logger to use to describe any error.</param>
        /// <param name="propertyName">Name of the property to set.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="sourceDescription">Optional description of the origin of the value to help troubleshooting.</param>
        /// <returns>True on success, false if any error occurs.</returns>
        bool SetAmbiantPropertyValue( IActivityLogger logger, string propertyName, object value, string sourceDescription = null );

        /// <summary>
        /// Sets how an ambient property on the Structured Object must be resolved (the property must exist, be writeable, and marked with <see cref="AmbientPropertyAttribute"/>).
        /// </summary>
        /// <param name="logger">The logger to use to describe any error.</param>
        /// <param name="propertyName">Name of the property to configure.</param>
        /// <param name="context">See <see cref="IStObjMutableReference.Context"/>.</param>
        /// <param name="type">See <see cref="IStObjMutableReference.Type"/>.</param>
        /// <param name="behavior">See <see cref="IStObjMutableReference.StObjRequirementBehavior"/>.</param>
        /// <param name="sourceDescription">Optional description of the origin of the call to help troubleshooting.</param>
        /// <returns>True on success, false if any error occurs.</returns>
        bool SetAmbiantPropertyConfiguration( IActivityLogger logger, string propertyName, string context, Type type, StObjRequirementBehavior behavior, string sourceDescription = null );

    }
}
