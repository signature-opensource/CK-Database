using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    internal class StObjSetupData : StObjSetupDataBase, IStObjSetupData, IMutableStObjSetupData
    {
        readonly IStObj _stObj;

        string _fullNameWithoutContext;
        string _versions;
        IReadOnlyList<IDependentItemRef> _requiresEx;
        IReadOnlyList<IDependentItemRef> _requiredByEx;
        IReadOnlyList<IDependentItemRef> _childrenEx;
        IReadOnlyList<IDependentItemGroupRef> _groupsEx;

        internal StObjSetupData( IActivityLogger logger, IStObj o, StObjSetupDataBase parent )
            : base( logger, o.ObjectType, parent )
        {
            _stObj = o;

            _fullNameWithoutContext = SetupNameAttribute.GetFullName( logger, false, o.ObjectType );
            _versions = VersionsAttribute.GetVersionsString( o.ObjectType );
            _requiresEx = new ReadOnlyListOnIList<IDependentItemRef>( Requires );
            _requiredByEx = new ReadOnlyListOnIList<IDependentItemRef>( RequiredBy );
            _childrenEx = new ReadOnlyListOnIList<IDependentItemRef>( Children );
            _groupsEx = new ReadOnlyListOnIList<IDependentItemGroupRef>( Groups );
        }

        public IStObj StObj
        {
            get { return _stObj; }
        }

        public string FullNameWithoutContext
        {
            get { return _fullNameWithoutContext; }
            set { _fullNameWithoutContext = value; }
        }

        public bool IsDefaultFullName
        {
            get { return ReferenceEquals( _fullNameWithoutContext, _stObj.ObjectType.FullName ); } 
        }

        public string FullName
        {
            get { return AmbientContractCollector.DisplayName( _stObj.Context, FullNameWithoutContext ); }
        }

        public string Versions
        {
            get { return _versions; }
            set { _versions = value; }
        }

        internal void ResolveTypes( IActivityLogger logger )
        {
            if( ItemType == null && ItemTypeName != null ) ItemType = SimpleTypeFinder.WeakDefault.ResolveType( ItemTypeName, true );
            if( DriverType == null && DriverTypeName != null ) DriverType = SimpleTypeFinder.WeakDefault.ResolveType( DriverTypeName, true );
        }

        internal IMutableDependentItemContainerTyped SetupItem { get; set; }

        IReadOnlyList<IDependentItemRef> IStObjSetupData.RequiredBy
        {
            get { return _requiresEx; }
        }

        IReadOnlyList<IDependentItemRef> IStObjSetupData.Requires
        {
            get { return _requiredByEx; }
        }

        IReadOnlyList<IDependentItemRef> IStObjSetupData.Children
        {
            get { return _childrenEx; }
        }

        IReadOnlyList<IDependentItemGroupRef> IStObjSetupData.Groups
        {
            get { return _groupsEx; }
        }

    }
}
