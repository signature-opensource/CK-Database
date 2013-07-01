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

        protected new SqlTableAttribute Attribute { get { return (SqlTableAttribute)base.Attribute; } }

        protected override void ConfigureMutableItem( IActivityLogger logger, IStObjMutableItem o )
        {
            if( Attribute.TableName != null ) o.SetDirectPropertyValue( logger, "TableName", Attribute.TableName );
            if( Attribute.Schema != null ) o.SetAmbiantPropertyValue( logger, "Schema", Attribute.Schema );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
            SetAutomaticSetupFullNamewithoutContext( logger, data, "SqlTable" );
            data.ItemType = typeof( SqlTableItem );
            data.DriverType = typeof( SqlTableSetupDriver );
        }

    }
}
