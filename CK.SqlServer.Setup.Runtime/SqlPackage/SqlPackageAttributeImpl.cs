#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlPackage\SqlPackageAttributeImpl.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlPackageAttributeImpl : SqlPackageAttributeImplBase, IStObjSetupConfigurator
    {
        public SqlPackageAttributeImpl( SqlPackageAttribute a )
            : base( a )
        {
        }

        protected new SqlPackageAttribute Attribute { get { return (SqlPackageAttribute)base.Attribute; } }

        protected override void ConfigureMutableItem( IActivityMonitor monitor, IStObjMutableItem o )
        {
            o.SetDirectPropertyValue( monitor, "HasModel", Attribute.HasModel );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityMonitor monitor, IMutableStObjSetupData data )
        {
            if( data.IsDefaultFullNameWithoutContext )
            {
                monitor.Info().Send( "SqlPackage class '{0}' uses its own full name as its SetupName.", data.FullNameWithoutContext );
            }
            data.ItemType = typeof( SqlPackageBaseItem );
            data.DriverType = typeof( SqlPackageBaseSetupDriver );
        }
    
    }
}
