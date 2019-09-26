using System.Collections.Generic;
using CK.Core;

namespace CK.Setup
{
    internal class StObjSetupData : StObjSetupDataRootClass, IStObjSetupData, IMutableStObjSetupData
    {
        readonly IStObjResult _stObj;

        string _fullNameWithoutContext;
        string _versions;

        internal StObjSetupData( IActivityMonitor monitor, IStObjResult o, StObjSetupDataRootClass parent )
            : base( monitor, o.ObjectType, parent )
        {
            _stObj = o;

            _fullNameWithoutContext = AttributesReader.GetFullName( monitor, false, o.ObjectType );
            _versions = AttributesReader.GetVersionsString( o.ObjectType );
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

        public bool IsDefaultFullNameWithoutContext => ReferenceEquals( _fullNameWithoutContext, _stObj.ObjectType.FullName );

        public string FullName => DefaultContextLocNaming.Format( _stObj.StObjMap.MapName, null, _fullNameWithoutContext );

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

    }
}
