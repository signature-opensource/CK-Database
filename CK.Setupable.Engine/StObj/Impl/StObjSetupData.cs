#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\StObj\Impl\StObjSetupData.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

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

        public IStObjResult StObj
        {
            get { return _stObj; }
        }

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

        public bool IsDefaultFullNameWithoutContext
        {
            get { return ReferenceEquals( _fullNameWithoutContext, _stObj.ObjectType.FullName ); } 
        }

        public string FullName
        {
            get { return DefaultContextLocNaming.Format( _stObj.Context.Context, null, _fullNameWithoutContext ); }
        }

        public string Versions
        {
            get { return _versions; }
            set { _versions = value; }
        }

        internal void ResolveItemAndDriverTypes( IActivityMonitor monitor )
        {
            if( ItemType == null && ItemTypeName != null ) ItemType = SimpleTypeFinder.WeakDefault.ResolveType( ItemTypeName, true );
            if( DriverType == null && DriverTypeName != null ) DriverType = SimpleTypeFinder.WeakDefault.ResolveType( DriverTypeName, true );
        }

        internal IStObjSetupItem SetupItem { get; set; }

        IReadOnlyList<IDependentItemRef> IStObjSetupData.RequiredBy
        {
            get { return (IReadOnlyList<IDependentItemRef>)RequiredBy; }
        }

        IReadOnlyList<IDependentItemRef> IStObjSetupData.Requires
        {
            get { return (IReadOnlyList<IDependentItemRef>)Requires; }
        }

        IReadOnlyList<IDependentItemRef> IStObjSetupData.Children
        {
            get { return (IReadOnlyList<IDependentItemRef>)Children; }
        }

        IReadOnlyList<IDependentItemGroupRef> IStObjSetupData.Groups
        {
            get { return (IReadOnlyList<IDependentItemGroupRef>)Groups; }
        }

    }
}
