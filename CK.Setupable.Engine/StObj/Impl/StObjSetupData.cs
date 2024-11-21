using System.Collections.Generic;
using CK.Core;

namespace CK.Setup;

internal class StObjSetupData : StObjSetupDataRootClass, IStObjSetupData, IMutableStObjSetupData
{
    readonly IStObjResult _stObj;

    string _fullNameWithoutContext;
    string _versions;

    internal StObjSetupData( IActivityMonitor monitor, IStObjResult o, StObjSetupDataRootClass parent )
        : base( monitor, o.ClassType, parent )
    {
        _stObj = o;

        _fullNameWithoutContext = AttributesReader.GetFullName( monitor, false, o.ClassType );
        _versions = AttributesReader.GetVersionsString( o.ClassType );
    }

    public IStObjResult StObj => _stObj;

    public string FullNameWithoutContext
    {
        get { return _fullNameWithoutContext; }
        set { _fullNameWithoutContext = value; }
    }

    public bool IsFullNameWithoutContextAvailable( string name )
    {
        IStObjSetupData g = Generalization;
        while( g != null )
        {
            if( g.FullNameWithoutContext == name ) return false;
            g = g.Generalization;
        }
        return true;
    }

    public bool IsDefaultFullNameWithoutContext => ReferenceEquals( _fullNameWithoutContext, _stObj.ClassType.FullName );

    public string FullName => DefaultContextLocNaming.Format( string.Empty, null, _fullNameWithoutContext );

    public string Versions
    {
        get { return _versions; }
        set { _versions = value; }
    }

    internal void ResolveItemAndDriverTypes( IActivityMonitor monitor )
    {
        if( ItemType == null && ItemTypeName != null ) ItemType = SimpleTypeFinder.WeakResolver( ItemTypeName, true );
        if( DriverType == null && DriverTypeName != null ) DriverType = SimpleTypeFinder.WeakResolver( DriverTypeName, true );
    }

    internal IStObjSetupItem SetupItem { get; set; }

    IReadOnlyList<IDependentItemRef> IStObjSetupData.RequiredBy => (IReadOnlyList<IDependentItemRef>)RequiredBy;

    IReadOnlyList<IDependentItemRef> IStObjSetupData.Requires => (IReadOnlyList<IDependentItemRef>)Requires;

    IReadOnlyList<IDependentItemRef> IStObjSetupData.Children => (IReadOnlyList<IDependentItemRef>)Children;

    IReadOnlyList<IDependentItemGroupRef> IStObjSetupData.Groups => (IReadOnlyList<IDependentItemGroupRef>)Groups;

    bool IStObjSetupData.SetDirectPropertyValue( IActivityMonitor monitor, string propertyName, object value, string sourceDescription )
    {
        return ((IStObjMutableItem)_stObj).SetDirectPropertyValue( monitor, propertyName, value, sourceDescription );
    }

}
