using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlTableAttribute : SqlPackageAttributeBase, IStObjSetupConfigurator
    {
        public SqlTableAttribute( string tableName )
        {
            TableName = tableName;
        }

        public string TableName { get; set; }

        protected override void ConfigureMutableItem( IActivityLogger logger, IStObjMutableItem o )
        {
            if( TableName != null ) o.SetDirectPropertyValue( logger, "TableName", TableName );
            if( Schema != null ) o.SetAmbiantPropertyValue( logger, "Schema", Schema );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
            SetAutomaticSetupFullNamewithoutContext( logger, data, "SqlTable" );
            data.ItemType = typeof( SqlTableItem );
            data.DriverType = typeof( SqlTableSetupDriver );
        }

    }
}
