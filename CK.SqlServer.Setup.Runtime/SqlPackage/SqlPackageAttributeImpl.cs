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

        protected override void ConfigureMutableItem( IActivityLogger logger, IStObjMutableItem o )
        {
            o.SetDirectPropertyValue( logger, "HasModel", Attribute.HasModel );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
            if( data.IsDefaultFullNameWithoutContext )
            {
                logger.Info( "SqlPackage class '{0}' uses its own full name as its SetupName.", data.FullNameWithoutContext );
            }
            data.ItemType = typeof( SqlPackageItem );
            data.DriverType = typeof( SqlPackageSetupDriver );
        }
    
    }
}
