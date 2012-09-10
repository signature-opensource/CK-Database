using System;

namespace CK.Setup
{

    public interface IMutableStObjSetupData : IStObjSetupDataBase
    {
        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        string FullNameWithoutContext { get; set; }

        /// <summary>
        /// Gets or sets the list of available versions and optional associated previous full names with a string like: "1.2.4, Previous.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0"
        /// The last version must NOT define a previous name since the last version is the current one (an <see cref="ArgumentException"/> will be thrown).
        /// </summary>
        string Versions { get; set; }

        /// <summary>
        /// Gets or sets the full name of the container.
        /// If the container is already defined at the <see cref="IStObj"/> level, names must match otherwise an error occurs.
        /// This allow name binding to an existing container or package that is not a Structure Object: it should be rarely used.
        /// </summary>
        /// <remarks>
        /// This is not inherited: it must be explicitely set for each object.
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
        /// Gets or sets whether this object must not be considered as a <see cref="IDependentItemContainer"/>: when true, no items 
        /// must be subordinated to this object.
        /// </summary>        
        bool NoContent { get; set; }

        /// <summary>
        /// Gets or sets whether a Model package is associated to this object. The Model is required by this object
        /// and by each and every Model associated to the objects that require this object.
        /// </summary>
        /// <remarks>
        /// This is not inherited.
        /// </remarks>
        bool HasModel { get; set; }

        /// <summary>
        /// Gets or sets the setup driver type (when set this masks the <see cref="DriverTypeName"/> property).
        /// This property is inherited.
        /// </summary>
        /// <remarks>
        /// When let to null (and no <see cref="DriverTypeName"/> is specified either), 
        /// the standard <see cref="PackageDriver"/> is used.
        /// </remarks>
        Type DriverType { get; set; }

        /// <summary>
        /// Gets or sets the assembly qualified name of the setup driver type.
        /// This property is inherited and is ignored if <see cref="DriverType"/> is specified.
        /// </summary>
        /// <remarks>
        /// When let to null (and no <see cref="DriverType"/> is specified either), 
        /// the standard <see cref="PackageDriver"/> is used.
        /// </remarks>
        string DriverTypeName { get; set; }



    }
}
