#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\SetupAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class SetupAttribute : Attribute, IAttributeSetupName, IStObjAttribute
    {
        string _name;
        Type _itemType;
        string _itemTypeName;
        Type _driverType;
        string _driverTypeName;
        string _containerFullName;
        TrackAmbientPropertiesMode _trackAmbientProperties;
        DependentItemKindSpec _setupItemKind;

        public SetupAttribute()
        {
        }

        /// <summary>
        /// Gets or sets the full name of the setup object.
        /// </summary>
        /// <remarks>
        /// This property is not inherited.
        /// </remarks>
        public string FullName
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets the type of the <see cref="IDependentItem"/> to use instead of the default <see cref="DynamicPackageItem"/>. 
        /// When set, this masks the <see cref="ItemTypeName"/> property,  otherwise ItemTypeName can be used to 
        /// designate a specific IDependentItem.
        /// This property is inherited.
        /// </summary>
        public Type ItemType
        {
            get { return _itemType; }
            set { _itemType = value; }
        }

        /// <summary>
        /// Gets or sets the assembly qualified type name of the <see cref="IDependentItem"/> to use instead of the default <see cref="DynamicPackageItem"/>. 
        /// This is used ONLY if <see cref="ItemType"/> is not set.
        /// This property is inherited.
        /// </summary>
        public string ItemTypeName
        {
            get { return _itemTypeName; }
            set { _itemTypeName = value; }
        }

        /// <summary>
        /// Gets or sets the setup driver type (when set this masks the <see cref="DriverTypeName"/> property).
        /// This property is inherited.
        /// </summary>
        /// <remarks>
        /// When let to null (and no <see cref="DriverTypeName"/> is specified either), 
        /// a standard <see cref="SetupDriver"/> is used.
        /// </remarks>
        public Type DriverType
        {
            get { return _driverType; }
            set { _driverType = value; }
        }

        /// <summary>
        /// Gets or sets the assembly qualified name of the setup driver type.
        /// This property is inherited and is ignored if <see cref="DriverType"/> is specified.
        /// </summary>
        /// <remarks>
        /// When let to null (and no <see cref="DriverType"/> is specified either), 
        /// the standard <see cref="SetupDriver"/> is used.
        /// </remarks>
        public string DriverTypeName
        {
            get { return _driverTypeName; }
            set { _driverTypeName = value; }
        }

        /// <summary>
        /// Gets or sets the name of the container.
        /// If the container is already defined at the <see cref="IStObj"/> level by the <see cref="IStObj.Container"/> property or via a construct parameter, names must 
        /// match otherwise an error occurs.
        /// This allow name binding to an existing container or package that is not a Structure Object: it should be rarely used.
        /// </summary>
        /// <remarks>
        /// This is not inherited: the container of a specialization is not, by default, the container of its base class.
        /// </remarks>
        public string ContainerFullName
        {
            get { return _containerFullName; }
            set { _containerFullName = value; }
        }

        /// <summary>
        /// Gets how Ambient Properties that reference the object must be tracked.
        /// </summary>
        public TrackAmbientPropertiesMode TrackAmbientProperties
        {
            get { return _trackAmbientProperties; }
            set { _trackAmbientProperties = value; }
        }

        /// <summary>
        /// Gets or sets how this object must be considered regarding other items: it can be a <see cref="DependentItemKind.Item"/>, 
        /// a <see cref="DependentItemKind.Group"/> or a <see cref="DependentItemKind.Container"/>.
        /// When let to <see cref="DependentItemKind.Unknown"/>, this property is inherited (it is eventually 
        /// considered as <see cref="DependentItemKind.Container"/> when not set).
        /// </summary>
        public DependentItemKindSpec ItemKind
        {
            get { return _setupItemKind; }
            set { _setupItemKind = value; }
        }

        /// <summary>
        /// Gets or sets the container typed object. This should be used instead of <see cref="ContainerFullName"/>.
        /// </summary>
        public Type Container { get; set; }

        Type[] IStObjAttribute.Requires { get { return null; } }

        Type[] IStObjAttribute.RequiredBy { get { return null; } }

        Type[] IStObjAttribute.Children { get { return null; } }

        Type[] IStObjAttribute.Groups { get { return null; } }

    }
}
