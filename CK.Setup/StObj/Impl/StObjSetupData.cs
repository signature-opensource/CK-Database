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


        internal StObjSetupData( IActivityLogger logger, IStObj o, StObjSetupDataBase parent )
            : base( logger, o.ObjectType, parent )
        {
            _stObj = o;

            _fullNameWithoutContext = SetupNameAttribute.GetFullName( logger, false, o.ObjectType );
            _versions = VersionsAttribute.GetVersionsString( o.ObjectType );
            _requiresEx = new ReadOnlyListOnIList<IDependentItemRef>( Requires );
            _requiredByEx = new ReadOnlyListOnIList<IDependentItemRef>( RequiredBy );
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
            get { return AmbiantContractCollector.DisplayName( _stObj.Context, FullNameWithoutContext ); }
        }

        public string Versions
        {
            get { return _versions; }
            set { _versions = value; }
        }

        internal StObjDynamicPackageItem SetupItem { get; private set; }

        internal StObjDynamicPackageItem CreateSetupItem()
        {
            return SetupItem = new StObjDynamicPackageItem( this );
        }

        IReadOnlyList<IDependentItemRef> IStObjSetupData.RequiredBy
        {
            get { return _requiresEx; }
        }

        IReadOnlyList<IDependentItemRef> IStObjSetupData.Requires
        {
            get { return _requiredByEx; }
        }

    }
}
