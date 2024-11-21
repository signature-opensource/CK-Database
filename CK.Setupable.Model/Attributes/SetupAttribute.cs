using System;

namespace CK.Core;


/// <summary>
/// Specializes <see cref="StObjAttribute"/> to define properties related to the three-steps setup: naming of the object,
/// type of the associated item and type of the setup driver.
/// <para>
/// All properties are inherited except the <see cref="ContainerFullName"/>: the container of a specialization is not, by default,
/// the container of its base class.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public class SetupAttribute : RealObjectAttribute, Setup.IAttributeSetupName
{

    /// <summary>
    /// Initializes a new empty <see cref="SetupAttribute"/>.
    /// </summary>
    public SetupAttribute()
    {
    }

    /// <summary>
    /// Gets or sets the name of the container.
    /// If the container is already defined at the <see cref="StObjAttribute"/> level by the <see cref="StObjAttribute.Container"/> property or via a construct parameter, names must 
    /// match otherwise an error occurs.
    /// This allow name binding to an existing container or package that is not a Structure Object: it should be rarely used.
    /// </summary>
    /// <remarks>
    /// This is not inherited: the container of a specialization is not, by default, the container of its base class.
    /// </remarks>
    public string ContainerFullName { get; set; }

    /// <summary>
    /// Gets or sets the full name of the setup object.
    /// </summary>
    /// <remarks>
    /// This property is not inherited.
    /// </remarks>
    public string FullName { get; set; }

    /// <summary>
    /// Gets or sets the type of the dependent item to use instead of the default SetupItem. 
    /// When set, this masks the <see cref="ItemTypeName"/> property,  otherwise ItemTypeName can be used to 
    /// designate a specific IDependentItem.
    /// This property is inherited.
    /// </summary>
    public Type ItemType { get; set; }

    /// <summary>
    /// Gets or sets the assembly qualified type name of the dependent item to use instead of the default SetupItem. 
    /// This is used ONLY if <see cref="ItemType"/> is not set.
    /// This property is inherited.
    /// </summary>
    public string ItemTypeName { get; set; }

    /// <summary>
    /// Gets or sets the setup driver type (when set this masks the <see cref="DriverTypeName"/> property).
    /// This property is inherited.
    /// </summary>
    /// <remarks>
    /// When let to null (and no <see cref="DriverTypeName"/> is specified either), 
    /// a standard SetupDriver is used.
    /// </remarks>
    public Type DriverType { get; set; }

    /// <summary>
    /// Gets or sets the assembly qualified name of the setup driver type.
    /// This property is inherited and is ignored if <see cref="DriverType"/> is specified.
    /// </summary>
    /// <remarks>
    /// When let to null (and no <see cref="DriverType"/> is specified either), 
    /// the standard SetupDriver is used.
    /// </remarks>
    public string DriverTypeName { get; set; }

}
