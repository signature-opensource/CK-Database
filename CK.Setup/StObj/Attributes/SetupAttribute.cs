using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class SetupAttribute : Attribute, ISetupNameAttribute, IStObjAttribute
    {
        string _name;
        Type _driverType;
        string _driverTypeName;
        string _containerFullName;
        bool? _noContent;
        bool _hasModel;

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
        /// Gets or sets the setup driver type (when set this masks the <see cref="DriverTypeName"/> property).
        /// This property is inherited.
        /// </summary>
        /// <remarks>
        /// When let to null (and no <see cref="DriverTypeName"/> is specified either), 
        /// the standard <see cref="PackageDriver"/> is used.
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
        /// the standard <see cref="PackageDriver"/> is used.
        /// </remarks>
        public string DriverTypeName
        {
            get { return _driverTypeName; }
            set { _driverTypeName = value; }
        }

        /// <summary>
        /// Gets or sets the name of the container.
        /// If the container is already defined at the <see cref="IStObj"/> level by the <see cref="Container"/> property or via a construct parameter, names must 
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
        /// Gets or sets whether this object must not be considered as a <see cref="IDependentItemContainer"/>: no items 
        /// must be subordinated to this object.
        /// This property is inherited.
        /// </summary>
        public bool NoContent
        {
            get { return _noContent ?? false; }
            set { _noContent = value; }
        }

        internal bool NoContentDefined
        {
            get { return _noContent.HasValue; }
        }

        /// <summary>
        /// Gets or sets whether a Model package is associated to this object. The Model is required by this object
        /// and by each and every Model associated to the objects that require this object.
        /// </summary>
        /// <remarks>
        /// This is not inherited.
        /// </remarks>
        public bool HasModel
        {
            get { return _hasModel; }
            set { _hasModel = value; }
        }

        /// <summary>
        /// Gets or sets the container typed object. This should be used instead of <see cref="ContainerFullName"/>.
        /// </summary>
        public Type Container { get; set; }

        Type[] IStObjAttribute.Requires { get { return null; } }

        Type[] IStObjAttribute.RequiredBy { get { return null; } }

        static internal SetupAttribute GetSetupAttribute( Type t )
        {
            return (SetupAttribute)t.GetCustomAttributes( typeof( SetupAttribute ), false ).SingleOrDefault();
        }

        internal static void ApplyAttributesConfigurator( IActivityLogger logger, Type t, StObjSetupData data )
        {
            var all = t.GetCustomAttributes( typeof( IStObjSetupConfigurator ), false );
            foreach( IStObjSetupConfigurator c in all )
            {
                c.ConfigureDependentItem( logger, data );
            }
        }
        

    }
}
