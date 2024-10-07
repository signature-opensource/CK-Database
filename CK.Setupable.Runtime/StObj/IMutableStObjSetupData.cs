#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\StObj\IMutableStObjSetupData.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Setup;


/// <summary>
/// Once StObj have been sorted and initialized, this interface enables any <see cref="IStObjSetupConfigurator"/> to
/// configure the setup item: this drives the type of the item itself (that defaults to <see cref="StObjDynamicPackageItem"/>) and most of its properties.
/// </summary>
public interface IMutableStObjSetupData : IStObjSetupDataBase
{
    /// <summary>
    /// Gets or sets the full name.
    /// </summary>
    string FullNameWithoutContext { get; set; }

    /// <summary>
    /// Checks if the proposed name can be the <see cref="FullNameWithoutContext"/>: false if one of our <see cref="IStObjSetupDataBase.Generalization"/> already uses this name.
    /// </summary>
    /// <param name="proposedName">Name to check.</param>
    /// <returns>True if the name can be used.</returns>
    bool IsFullNameWithoutContextAvailable( string proposedName );

    /// <summary>
    /// Gets or sets the list of available versions and optional associated previous full names with a string like: "1.2.4, Previous.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0"
    /// The last version must NOT define a previous name since the last version is the current one (an <see cref="ArgumentException"/> will be thrown).
    /// </summary>
    string Versions { get; set; }

    /// <summary>
    /// Gets or sets the full name of the container.
    /// This overrides the container that may have been already defined at the <see cref="IStObjResult"/> level.
    /// This allow name binding to another container or package, even one that is not a Structure Object: it should be rarely used and most often let to null.
    /// </summary>
    /// <remarks>
    /// This is not inherited: it must be explicitly set for each object.
    /// </remarks>
    string ContainerFullName { get; set; }

    /// <summary>
    /// Gets a mutable list of requirements (can be <see cref="IDependentItem"/> instances or named references).
    /// </summary>
    IDependentItemList Requires { get; }

    /// <summary>
    /// Gets a mutable list of reverse requirements (can be <see cref="IDependentItem"/> instances or named references).
    /// </summary>
    IDependentItemList RequiredBy { get; }

    /// <summary>
    /// Gets a mutable list of children (can be <see cref="IDependentItem"/> instances or named references).
    /// </summary>
    IDependentItemList Children { get; }

    /// <summary>
    /// Gets or sets the type of the <see cref="IDependentItem"/> to use instead of the default <see cref="StObjDynamicPackageItem"/>. 
    /// When set, this masks the <see cref="ItemTypeName"/> property,  otherwise ItemTypeName can be used to 
    /// designate a specific IDependentItem type.
    /// This property is inherited.
    /// </summary>
    Type ItemType { get; set; }

    /// <summary>
    /// Gets or sets the assembly qualified type name of the <see cref="IDependentItem"/> to use instead of the default <see cref="StObjDynamicPackageItem"/>. 
    /// This is used ONLY if <see cref="ItemType"/> is not set.
    /// This property is inherited.
    /// </summary>
    string ItemTypeName { get; set; }

    /// <summary>
    /// Gets or sets the setup driver type (when set this masks the <see cref="DriverTypeName"/> property).
    /// This is used ONLY if <see cref="ItemType"/> and <see cref="ItemTypeName"/> are not set.
    /// This enables the use of a specialized <see cref="SetupItemDriver"/> bound to a default <see cref="StObjDynamicPackageItem"/>.
    /// This property is inherited.
    /// </summary>
    /// <remarks>
    /// When let to null (and no <see cref="DriverTypeName"/> is specified either), 
    /// a standard <see cref="SetupItemDriver"/> is used.
    /// </remarks>
    Type DriverType { get; set; }

    /// <summary>
    /// Gets or sets the assembly qualified name of the setup driver type.
    /// This is used ONLY if <see cref="ItemType"/>, <see cref="ItemTypeName"/> and <see cref="DriverType"/> are not set.
    /// This is the ultimate fallback in order to use anything else than the default <see cref="SetupItemDriver"/> (bound to a default <see cref="StObjDynamicPackageItem"/>).
    /// This property is inherited and is ignored if <see cref="DriverType"/> is specified.
    /// </summary>
    /// <remarks>
    /// When let to null (and no <see cref="DriverType"/> is specified either), 
    /// a standard <see cref="SetupItemDriver"/> is used.
    /// </remarks>
    string DriverTypeName { get; set; }

}
