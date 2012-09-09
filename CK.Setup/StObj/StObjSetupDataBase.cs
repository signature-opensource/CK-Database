using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class StObjSetupDataBase
    {
        readonly IStObjSetupData _parent;

        Type _driverType;
        string _driverTypeName;
        string _containerFullName;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        bool _hasModel;
        bool _noContent;

        internal StObjSetupDataBase( IActivityLogger logger, Type t, StObjSetupDataBase parent = null )
        {
            _parent = parent as IStObjSetupData;
            bool isInRoot = _parent == null;

            _requires = RequiresAttribute.GetRequirements( logger, t, typeof( RequiresAttribute ) );
            _requiredBy = RequiresAttribute.GetRequirements( logger, t, typeof( RequiredByAttribute ) );
            SetupAttribute setupAttr = SetupAttribute.GetSetupAttribute( t );
            if( setupAttr != null )
            {
                _containerFullName = setupAttr.ContainerFullName;
                _driverType = setupAttr.DriverType;
                _driverTypeName = setupAttr.DriverTypeName;
                
                _hasModel = setupAttr.HasModel;
                
                if( setupAttr.NoContentDefined ) _noContent = setupAttr.NoContent;
                else _noContent = parent != null ? parent.NoContent : false;
            }
            else
            {
                // No Package attribute...
                if( parent != null )
                {
                    // _hasModel remains false (this is not because the base class has an associated Model that
                    // a specialization has one), but NoContent is stronger: if a base class states that it has no content,
                    // its specialization, by default, also reject content.
                    _noContent = parent.NoContent;
                    
                    // We accept to inherit from parent HasModel, only if 
                    // we are the root ambiant contract (consider attributes on above classes to 
                    // be kind of "definer").
                    if( isInRoot )
                    {
                        _hasModel = parent.HasModel;
                        // There is currently no other attribute that works this way
                        // (Requirements are handled below, full name and versions are fundamentally by StObj).
                    }
                }
            }
            // Container full name, driver type & name inherit by default.
            if( parent != null )
            {
                if( _driverType == null ) _driverType = parent.DriverType;
                if( _driverTypeName == null ) _driverTypeName = parent.DriverTypeName;
                if( _containerFullName == null ) _containerFullName = parent.ContainerFullName;
            }

            // If we are the root of the ambiant contract, we consider that base classes
            // preinitialize our value.
            if( isInRoot && parent != null )
            {
                Requires.AddRange( parent.Requires );
                RequiredBy.AddRange( parent.RequiredBy );
            }
        }

        /// <summary>
        /// Gets the parent setup data if it exists (this is to manage attribute properties "inheritance"). 
        /// Null if this object corresponds to the first (top) <see cref="IAmbiantContract"/> of the inheritance chain.
        /// </summary>
        public IStObjSetupData Parent
        {
            get { return _parent; }
        }
        
        /// <summary>
        /// Gets or sets the full name of the container.
        /// If the container is already defined at the <see cref="IStObj"/> level, names must match otherwise an error occurs.
        /// This allow name binding to an existing container or package that is not a Structure Object: it should be rarely used.
        /// </summary>
        /// <remarks>
        /// This is not inherited: it must be explicitely set for each object.
        /// </remarks>
        public string ContainerFullName
        {
            get { return _containerFullName; }
            set { _containerFullName = value; }
        }

        /// <summary>
        /// Gets a mutable list of requirements (can be <see cref="IDependentItem"/> instances or named references).
        /// </summary>
        public IDependentItemList Requires
        {
            get { return _requires; }
        }

        /// <summary>
        /// Gets a mutable list of reverse requirements (can be <see cref="IDependentItem"/> instances or named references).
        /// </summary>
        public IDependentItemList RequiredBy
        {
            get { return _requiredBy; }
        }

        /// <summary>
        /// Gets or sets whether this object must not be considered as a <see cref="IDependentItemContainer"/>: when true, no items 
        /// must be subordinated to this object.
        /// </summary>        
        public bool NoContent
        {
            get { return _noContent; }
            set { _noContent = value; }
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


        internal static StObjSetupDataBase CreateRootData( IActivityLogger logger, Type t )
        {
            if( t == typeof( object ) ) return null;
            StObjSetupDataBase b = CreateRootData( logger, t.BaseType );
            return new StObjSetupDataBase( logger, t, b ); 
        }
    }
}
