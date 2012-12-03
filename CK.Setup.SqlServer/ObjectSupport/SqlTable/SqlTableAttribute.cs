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
            if( data.IsDefaultFullNameWithoutContext )
            {
                var table = (SqlTable)data.StObj.Object;
                var autoName = table.SchemaName;
                if( data.IsFullNameWithoutContextAvailable( autoName ) )
                {
                    logger.Info( "SqlTable '{0}' uses its own table name '{1}' as its SetupName.", data.StObj.ObjectType.FullName, autoName );
                }
                else
                {
                    autoName = FindAvailableFullNameWithoutContext( data, autoName );
                    logger.Info( "SqlTable '{0}' has no defined SetupName. It has been automatically computed as '{1}'. You may set a [SetupName] attribute on the class to settle it.", data.StObj.ObjectType.FullName, autoName );
                }
                data.FullNameWithoutContext = autoName;
            }
            data.ItemType = typeof( SqlTableItem );
            data.DriverType = typeof( SqlTableSetupDriver );
            data.HasModel = true;
        }

    }
}
