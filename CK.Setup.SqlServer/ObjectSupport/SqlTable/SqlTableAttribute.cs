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
            if( TableName != null ) o.SetPropertyStructuralValue( logger, "SqlTableAttribute", "TableName", TableName );
            if( Schema != null ) o.SetPropertyStructuralValue( logger, "SqlTableAttribute", "Schema", Schema );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
            if( data.IsDefaultFullName )
            {
                var table = (SqlTable)data.StObj.Object;
                logger.Info( "Class '{0}' uses its own table name '{1}' as its Setup FullName.", data.StObj.ObjectType.Name, table.SchemaName );
                data.FullNameWithoutContext = table.SchemaName;
            }
            data.ItemType = typeof( SqlTableItem );
            data.DriverType = typeof( SqlTableSetupDriver );
            data.HasModel = true;
        }

    }
}
