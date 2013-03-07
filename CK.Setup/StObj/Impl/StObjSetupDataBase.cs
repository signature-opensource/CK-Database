using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    internal class StObjSetupDataBase
    {
        readonly IStObjSetupData _parent;

        Type _itemType;
        string _itemTypeName;
        Type _driverType;
        string _driverTypeName;
        string _containerFullName;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemList _children;
        DependentItemGroupList _groups;

        internal StObjSetupDataBase( IActivityLogger logger, Type t, StObjSetupDataBase parent = null )
        {
            _parent = parent as IStObjSetupData;
            bool isInRoot = _parent == null;

            _requires = RequiresAttribute.GetRequirements( logger, t, typeof( RequiresAttribute ) );
            _requiredBy = RequiresAttribute.GetRequirements( logger, t, typeof( RequiredByAttribute ) );
            _children = RequiresAttribute.GetRequirements( logger, t, typeof( ChildrenAttribute ) );
            _groups = GroupsAttribute.GetGroups( logger, t );
            SetupAttribute setupAttr = SetupAttribute.GetSetupAttribute( t );
            if( setupAttr != null )
            {
                _containerFullName = setupAttr.ContainerFullName;
                _itemType = setupAttr.ItemType;
                _itemTypeName = setupAttr.ItemTypeName;
                _driverType = setupAttr.DriverType;
                _driverTypeName = setupAttr.DriverTypeName;
            }
            // Container full name, driver type & name inherit by default.
            if( parent != null )
            {
                if( _itemType == null && _itemTypeName == null ) _itemType = parent.ItemType;
                if( _itemTypeName == null ) _itemTypeName = parent.ItemTypeName;
                if( _driverType == null && _driverTypeName == null ) _driverType = parent.DriverType;
                if( _driverTypeName == null ) _driverTypeName = parent.DriverTypeName;
                if( _containerFullName == null ) _containerFullName = parent.ContainerFullName;
            }

            // If we are the root of the ambient contract, we consider that base classes
            // preinitialize our value.
            if( isInRoot && parent != null )
            {
                _requires.AddRange( parent.Requires );
                _requiredBy.AddRange( parent.RequiredBy );
                _children.AddRange( parent.Children );
                _groups.AddRange( parent.Groups );
            }
        }

        public IStObjSetupData Generalization
        {
            get { return _parent; }
        }
        
        public string ContainerFullName
        {
            get { return _containerFullName; }
            set { _containerFullName = value; }
        }

        public IDependentItemList Requires
        {
            get { return _requires; }
        }

        public IDependentItemList RequiredBy
        {
            get { return _requiredBy; }
        }

        public IDependentItemList Children
        {
            get { return _children; }
        }

        public IDependentItemGroupList Groups
        {
            get { return _groups; }
        }

        public Type ItemType
        {
            get { return _itemType; }
            set { _itemType = value; }
        }

        public string ItemTypeName
        {
            get { return _itemTypeName; }
            set { _itemTypeName = value; }
        }

        public Type DriverType
        {
            get { return _driverType; }
            set { _driverType = value; }
        }

        public string DriverTypeName
        {
            get { return _driverTypeName; }
            set { _driverTypeName = value; }
        }

        internal static StObjSetupDataBase CreateRootData( IActivityLogger logger, Type t )
        {
            if( t == typeof( object ) ) return null;
            StObjSetupDataBase b = CreateRootData( logger, t.BaseType );
            return new StObjSetupDataBase( logger, t, b ); 
        }
    }
}
