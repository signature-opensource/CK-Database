using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlObjectItemAttribute : Attribute
    {
        public SqlObjectItemAttribute( string objectName )
        {
            ObjectName = objectName;
        }

        public string ObjectName { get; private set; }

        public SqlObjectItem Create( IActivityLogger logger, IStObjSetupData container )
        {
            if( !(container.StObj.Object is SqlPackageBase) )
            {
                throw new NotSupportedException( "SqlObjectItemAttribute must be set only on class that inherits SqlPackageBase." );
            }
            SqlPackageBase p = (SqlPackageBase)container.StObj.Object;
            string fileName = ObjectName + ".sql";
            string text = p.ResourceLocation.GetString( ObjectName, true );
            SqlObjectProtoItem protoObject = SqlObjectParser.Create( logger, text );
            if( protoObject == null ) return null;
            if( protoObject.Name != ObjectName )
            {
                logger.Error( "Resource '{0}' contains the definition of '{1}'. Names must match.", fileName, protoObject.Name );
                return null;
            }
            if( protoObject.Schema.Length > 0 && protoObject.Schema != p.Schema )
            {
                logger.Error( "Resource '{0}' defines the {1} in the schema '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.Schema, p.Schema );
                return null;
            }
            // For Database, if specified, we can not force the 
            if( protoObject.DatabaseName.Length > 0 && protoObject.DatabaseName != p.Database.Name )
            {
                logger.Error( "Resource '{0}' defines the {1} in the database '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.DatabaseName, p.Database.Name );
                return null;
            }
            return protoObject.CreateItem( logger );
        }
    }
}
