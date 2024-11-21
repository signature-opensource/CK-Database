using CK.Core;
using System;
using System.Collections.Generic;


namespace CK.Setup;

/// <summary>
/// Wraps a <see cref="IStObjResult"/> StObj with information related to the ordering phases and supports
/// the capacity to set a property on this <see cref="IStObjSetupDataBase.StObj"/>'s final object.
/// </summary>
public interface IStObjSetupData : IStObjSetupDataBase
{
    /// <summary>
    /// Gets the full name without its context. 
    /// </summary>
    string FullNameWithoutContext { get; }

    /// <summary>
    /// Gets the list of available versions and optional associated previous full names with a string like: "1.2.4, Previous.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0"
    /// </summary>
    string Versions { get; }

    /// <summary>
    /// Gets the full name of the container.
    /// This overrides the container that may have been already defined at the <see cref="IStObjResult"/> level.
    /// </summary>
    string ContainerFullName { get; }

    /// <summary>
    /// Gets the type of the <see cref="IMutableSetupItem"/> to use instead of the default <see cref="StObjDynamicPackageItem"/>. 
    /// When set, this masks the <see cref="ItemTypeName"/> property,  otherwise <see cref="ItemTypeName"/> can be used to 
    /// designate a specific <see cref="IMutableSetupItem"/> by its assembly qualified name.
    /// This property is inherited.
    /// </summary>
    Type ItemType { get; }

    /// <summary>
    /// Gets the assembly qualified type name of the <see cref="IMutableSetupItem"/> to use instead of the default <see cref="StObjDynamicPackageItem"/>. 
    /// This is used ONLY if <see cref="ItemType"/> is not set.
    /// This property is inherited.
    /// </summary>
    string ItemTypeName { get; }

    /// <summary>
    /// Gets setup driver type (when not null this masks the <see cref="DriverTypeName"/> property).
    /// This enables the use of a specialized <see cref="SetupItemDriver"/> bound to a default <see cref="StObjDynamicPackageItem"/>.
    /// This property is inherited.
    /// </summary>
    /// <remarks>
    /// When let to null (and no <see cref="DriverTypeName"/> is specified either), 
    /// the standard <see cref="SetupItemDriver"/> is used.
    /// </remarks>
    Type DriverType { get; }

    /// <summary>
    /// Gets the assembly qualified name of the setup driver type.
    /// This is the ultimate fallback in order to use anything else than the default <see cref="SetupItemDriver"/> (bound to a default <see cref="StObjDynamicPackageItem"/>).
    /// This property is inherited.
    /// </summary>
    /// <remarks>
    /// When let to null (and no <see cref="DriverType"/> is specified either), 
    /// the standard <see cref="SetupItemDriver"/> is used.
    /// </remarks>
    string DriverTypeName { get; }

    /// <summary>
    /// Gets the list of requirements (can be <see cref="IDependentItem"/> instances or named references).
    /// </summary>
    IReadOnlyList<IDependentItemRef> Requires { get; }

    /// <summary>
    /// Gets the list of reverse requirements (can be <see cref="IDependentItem"/> instances or named references).
    /// </summary>
    IReadOnlyList<IDependentItemRef> RequiredBy { get; }

    /// <summary>
    /// Gets the list of groups (can be <see cref="IDependentItemGroup"/> instances or named references).
    /// </summary>
    IReadOnlyList<IDependentItemGroupRef> Groups { get; }

    /// <summary>
    /// Gets the list of children (can be <see cref="IDependentItem"/> instances or named references).
    /// </summary>
    IReadOnlyList<IDependentItemRef> Children { get; }

    /// <summary>
    /// Sets a direct property (it must not be an Ambient Property, Singleton nor a StObj property) on the final Object. 
    /// The property must exist, be writable and the type of the <paramref name="value"/> must be compatible with the property type 
    /// otherwise an error is logged.
    /// </summary>
    /// <param name="monitor">The monitor to use to describe any error.</param>
    /// <param name="propertyName">Name of the property to set.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="sourceDescription">Optional description of the origin of the value to help troubleshooting.</param>
    /// <returns>True on success, false if any error occurs.</returns>
    bool SetDirectPropertyValue( IActivityMonitor monitor, string propertyName, object value, string sourceDescription = null );

}
