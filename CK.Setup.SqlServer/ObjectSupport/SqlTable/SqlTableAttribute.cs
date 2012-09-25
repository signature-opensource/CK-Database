using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false )]
    public class SqlTableAttribute : Attribute, IStObjStructuralConfigurator, IStObjSetupConfigurator
    {
        public SqlTableAttribute( string tableName )
        {
            TableName = tableName;
        }

        /// <summary>
        /// Gets or sets the <see cref="SqlDatabase"/> that owns this table.
        /// See <see cref="SqlPackageAttribute.Database"/>.
        /// </summary>
        public Type Database { get; set; }
        
        public Type Package { get; set; }

        public string TableName { get; set; }

        public string Schema { get; set; }

        void IStObjStructuralConfigurator.Configure( IActivityLogger logger, IStObjMutableItem o )
        {
            if( !typeof( SqlTableType ).IsAssignableFrom( o.ObjectType.BaseType ) )
            {
                logger.Error( "Attribute SqlTable must be set only on class that specialize SqlTableType." );
            }
            if( Package != null )
            {
                if( o.Container.Type == null ) o.Container.Type = Package;
                else if( o.Container.Type != Package )
                {
                    logger.Error( "{0}: SqlTable attribute sets Package to be '{1}' but it is already '{2}'.", o.ToString(), Package.Name, o.Container.Type );
                }
            }
            if( Database != null )
            {
                var ambiant = o.AllAmbiantProperties.First( a => a.Name == "Database" );
                ambiant.StObjRequirementBehavior = StObjRequirementBehavior.WarnIfNotStObj;
                ambiant.Type = Database;
            }
            if( TableName != null ) o.SetPropertyStructuralValue( logger, "SqlTableAttribute", "TableName", TableName );
            if( Schema != null ) o.SetPropertyStructuralValue( logger, "SqlTableAttribute", "Schema", Schema );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
            if( data.IsDefaultFullName )
            {
                var table = (SqlTableType)data.StObj.StructuredObject;
                logger.Info( "Class '{0}' uses its own table name '{1}' as its Setup FullName.", data.StObj.ObjectType.Name, table.SchemaName );
                data.FullNameWithoutContext = table.SchemaName;
            }
            data.HasModel = true;
        }

    }
}
