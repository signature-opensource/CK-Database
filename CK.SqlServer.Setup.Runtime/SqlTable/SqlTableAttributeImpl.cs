#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlTable\SqlTableAttributeImpl.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Implementation of <see cref="SqlTableAttribute"/>.
    /// </summary>
    public class SqlTableAttributeImpl : SqlPackageAttributeImplBase, IStObjSetupConfigurator
    {
        /// <summary>
        /// Initializes a new <see cref="SqlTableAttributeImpl"/>.
        /// </summary>
        /// <param name="a">The attribute.</param>
        public SqlTableAttributeImpl( SqlTableAttribute a )
            : base( a )
        {
        }

        /// <summary>
        /// Masked to formally associates a <see cref="SqlTableAttribute"/> attribute.
        /// </summary>
        protected new SqlTableAttribute Attribute => (SqlTableAttribute)base.Attribute;

        /// <summary>
        /// Transfers <see cref="SqlTableAttribute.TableName" /> as a direct property "TableName" of the StObj item
        /// and <see cref="SqlPackageAttributeBase.Schema"/> as the "Schema" ambient property.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="o">The configured object.</param>
        protected override void ConfigureMutableItem( IActivityMonitor monitor, IStObjMutableItem o )
        {
            if( Attribute.TableName != null ) o.SetDirectPropertyValue( monitor, "TableName", Attribute.TableName );
            if( Attribute.Schema != null ) o.SetAmbientPropertyValue( monitor, "Schema", Attribute.Schema );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityMonitor monitor, IMutableStObjSetupData data )
        {
            SetAutomaticSetupFullNameWithoutContext( monitor, data, "SqlTable" );
            // Since we are THE SqlTable attribute, if a SetupItem or a Driver has been configured 
            // we consider that the configuration must be specific: this acts as a kind of default.
            if( data.ItemType == null && data.ItemTypeName == null )
            {
                data.ItemType = typeof( SqlTableItem );
            }
            if( data.DriverType == null && data.DriverTypeName == null )
            {
                data.DriverType = typeof( SqlTableItemDriver );
            }
        }

    }
}
