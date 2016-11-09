#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlTable\SqlTableAttributeImpl.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlTableAttributeImpl : SqlPackageAttributeImplBase, IStObjSetupConfigurator
    {
        public SqlTableAttributeImpl( SqlTableAttribute a )
            : base( a )
        {
        }

        protected new SqlTableAttribute Attribute => (SqlTableAttribute)base.Attribute; 

        protected override void ConfigureMutableItem( IActivityMonitor monitor, IStObjMutableItem o )
        {
            if( Attribute.TableName != null ) o.SetDirectPropertyValue( monitor, "TableName", Attribute.TableName );
            if( Attribute.Schema != null ) o.SetAmbiantPropertyValue( monitor, "Schema", Attribute.Schema );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityMonitor monitor, IMutableStObjSetupData data )
        {
            SetAutomaticSetupFullNamewithoutContext( monitor, data, "SqlTable" );
            data.ItemType = typeof( SqlTableItem );
            data.DriverType = typeof( SqlTableItemDriver );
        }

    }
}
