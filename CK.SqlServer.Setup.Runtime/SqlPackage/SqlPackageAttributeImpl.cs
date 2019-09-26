#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlPackage\SqlPackageAttributeImpl.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Implements <see cref="SqlPackageAttribute"/> attribute.
    /// </summary>
    public class SqlPackageAttributeImpl : SqlPackageAttributeImplBase, IStObjSetupConfigurator
    {
        /// <summary>
        /// Initializes a new <see cref="SqlPackageAttribute"/>.
        /// </summary>
        /// <param name="a">The attribute.</param>
        public SqlPackageAttributeImpl( SqlPackageAttribute a )
            : base( a )
        {
        }

        /// <summary>
        /// Masked to be formally associated to the <see cref="SqlPackageAttribute"/> attribte type.
        /// </summary>
        protected new SqlPackageAttribute Attribute => (SqlPackageAttribute)base.Attribute; 

        /// <summary>
        /// Transfers <see cref="SqlPackageAttribute.HasModel"/> to "HasModel" stobj property.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="o">The configured object.</param>
        protected override void ConfigureMutableItem( IActivityMonitor monitor, IStObjMutableItem o )
        {
            o.SetStObjPropertyValue( monitor, "HasModel", Attribute.HasModel );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityMonitor monitor, IMutableStObjSetupData data )
        {
            if( data.IsDefaultFullNameWithoutContext )
            {
                monitor.Info( $"SqlPackage '{data.FullNameWithoutContext}' uses its own full name as its SetupName." );
            }
            if( data.ItemType == null && data.ItemTypeName == null )
            {
                data.ItemType = typeof( SqlPackageBaseItem );
            }
            if( data.DriverType == null && data.DriverTypeName == null )
            {
                data.DriverType = typeof( SqlPackageBaseItemDriver );
            }
        }

    }
}
