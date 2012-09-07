using System;
using CK.Core;


namespace CK.Setup
{

    public interface IStObjSetupData
    {
        /// <summary>
        /// Gets the parent setup data if it exists (this is to manage attribute properties "inheritance"). 
        /// Null if this object corresponds to the first (top) <see cref="IAmbiantContract"/> of the inheritance chain.
        /// </summary>
        IStObjSetupData Parent { get; }
        
        /// <summary>
        /// Gets the associated <see cref="IStObj"/>.
        /// Never null.
        /// </summary>
        IStObj StObj { get; }

        /// <summary>
        /// Gets the [contextualized] full name of the object.
        /// </summary>
        string FullName { get; }
        
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
        /// If the container is already defined at the <see cref="IStObj"/> level, names must match otherwise an error occurs.
        /// This allow name binding to an existing container or package that is not a Structure Object: it should be rarely used.
        /// </summary>
        /// <remarks>
        /// This is not inherited: the container of a specialization is not, by default, the container of its base class.
        /// </remarks>
        string FullNameContainer { get; }

        /// <summary>
        /// Gets setup driver type (when not null this masks the <see cref="DriverTypeName"/> property).
        /// This property is inherited.
        /// </summary>
        /// <remarks>
        /// When let to null (and no <see cref="DriverTypeName"/> is specified either), 
        /// the standard <see cref="PackageDriver"/> is used.
        /// </remarks>
        Type DriverType { get; }

        /// <summary>
        /// Gets the assembly qualified name of the setup driver type.
        /// This property is inherited and is ignored if <see cref="DriverType"/> is specified.
        /// </summary>
        /// <remarks>
        /// When let to null (and no <see cref="DriverType"/> is specified either), 
        /// the standard <see cref="PackageDriver"/> is used.
        /// </remarks>
        string DriverTypeName { get; }

        /// <summary>
        /// Gets whether a Model package is associated to this object. The Model is required by this object
        /// and by each and every Model associated to the objects that require this object.
        /// </summary>
        /// <remarks>
        /// This is not inherited.
        /// </remarks>
        bool HasModel { get; }
        
        /// <summary>
        /// Gets whether this object must not be considered as a <see cref="IDependentItemContainer"/>: no items 
        /// must be subordinated to this object.
        /// This property is inherited.
        /// </summary>
        bool NoContent { get; }

        /// <summary>
        /// Gets the list of requirements (can be <see cref="IDependentItem"/> instances or named references).
        /// </summary>
        IReadOnlyList<IDependentItemRef> RequiredBy { get; }

        /// <summary>
        /// Gets the list of reverse requirements (can be <see cref="IDependentItem"/> instances or named references).
        /// </summary>
        IReadOnlyList<IDependentItemRef> Requires { get; }
    }
}
