using CK.Core;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CK.Setup;

internal class StObjSetupDataRootClass
{
    readonly IStObjSetupData _parent;

    Type _itemType;
    string _itemTypeName;
    Type _driverType;
    string _driverTypeName;
    string _containerFullName;
    IDependentItemList _requires;
    IDependentItemList _requiredBy;
    IDependentItemList _children;
    IDependentItemGroupList _groups;

    internal StObjSetupDataRootClass( IActivityMonitor monitor, Type t, StObjSetupDataRootClass parent = null )
    {
        _parent = parent as IStObjSetupData;
        bool isInRoot = _parent == null;

        _requires = DependentItemListFactory.CreateItemList();
        _requiredBy = DependentItemListFactory.CreateItemList();
        _groups = DependentItemListFactory.CreateItemGroupList();
        _children = DependentItemListFactory.CreateItemList();
        foreach( var a in t.CustomAttributes )
        {
            Type aType = a.AttributeType;
            if( aType == typeof( RequiresAttribute ) )
            {
                HandleMultiName( a, _requires.AddCommaSeparatedString );
            }
            else if( aType == typeof( RequiredByAttribute ) )
            {
                HandleMultiName( a, _requiredBy.AddCommaSeparatedString );
            }
            else if( aType == typeof( GroupsAttribute ) )
            {
                HandleMultiName( a, _groups.AddCommaSeparatedString );
            }
            else if( aType == typeof( ChildrenAttribute ) )
            {
                HandleMultiName( a, _children.AddCommaSeparatedString );
            }
        }

        static void HandleMultiName( CustomAttributeData a, Action<string> c )
        {
            var commaSeparatedPackageFullnames = (string[])a.ConstructorArguments[0].Value!;
            foreach( var n in commaSeparatedPackageFullnames )
            {
                c( n );
            }
        }

        SetupAttribute setupAttr = AttributesReader.GetSetupAttribute( t );
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

        // If we are the root of the real object, we consider that base classes
        // preinitialize our value.
        if( isInRoot && parent != null )
        {
            _requires.AddRange( parent.Requires );
            _requiredBy.AddRange( parent.RequiredBy );
            _children.AddRange( parent.Children );
            _groups.AddRange( parent.Groups );
        }
    }

    public IStObjSetupData Generalization => _parent;

    public string ContainerFullName
    {
        get { return _containerFullName; }
        set { _containerFullName = value; }
    }

    public IDependentItemList Requires => _requires;

    public IDependentItemList RequiredBy => _requiredBy;

    public IDependentItemList Children => _children;

    public IDependentItemGroupList Groups => _groups;

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

    internal static StObjSetupDataRootClass CreateRootData( IActivityMonitor monitor, Type t )
    {
        if( t == typeof( object ) ) return null;
        StObjSetupDataRootClass b = CreateRootData( monitor, t.BaseType );
        return new StObjSetupDataRootClass( monitor, t, b );
    }
}
