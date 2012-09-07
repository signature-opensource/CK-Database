using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class StObjSetupData : StObjSetupDataBase, IStObjSetupData
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

        /// <summary>
        /// Gets the associated <see cref="IStObj"/>.
        /// Never null.
        /// </summary>
        public IStObj StObj
        {
            get { return _stObj; }
        }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        public string FullNameWithoutContext
        {
            get { return _fullNameWithoutContext; }
            set { _fullNameWithoutContext = value; }
        }

        /// <summary>
        /// The default full name is the <see cref="Type.FullName"/>.
        /// </summary>
        public bool IsDefaultFullName
        {
            get { return ReferenceEquals( _fullNameWithoutContext, _stObj.ObjectType.FullName ); } 
        }

        /// <summary>
        /// Gets the [contextualized] full name of the object.
        /// </summary>
        public string FullName
        {
            get { return AmbiantContractCollector.DisplayName( _stObj.Context, FullNameWithoutContext ); }
        }

        /// <summary>
        /// Gets or sets the list of available versions and optional associated previous full names with a string like: "1.2.4, Previous.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0"
        /// The last version must NOT define a previous name since the last version is the current one (an <see cref="ArgumentException"/> will be thrown).
        /// </summary>
        public string Versions
        {
            get { return _versions; }
            set { _versions = value; }
        }

        internal StObjDynamicPackageItem SetupItem { get; private set; }

        internal void CreateSetupItem()
        {
            SetupItem = new StObjDynamicPackageItem( this );
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
