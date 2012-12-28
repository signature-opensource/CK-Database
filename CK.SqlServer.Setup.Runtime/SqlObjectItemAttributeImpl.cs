using System;
using System.Diagnostics;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Declares a resource that contains a Sql procedure, function or view associated to a type.
    /// Multiples object names like "sUserCreate, sUserDestroy, sUserUpgrade" can be defined.
    /// </summary>
    public class SqlObjectItemAttributeImpl : IStObjSetupDynamicInitializer
    {
        readonly SqlObjectItemAttribute Attribute;

        public SqlObjectItemAttributeImpl( SqlObjectItemAttribute a )
        {
            Attribute = a;
        }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IActivityLogger logger, IMutableSetupItem item, IStObj stObj )
        {
            if( !(stObj.Object is SqlPackageBase) )
            {
                throw new NotSupportedException( "SqlObjectItemAttribute must be set only on class that inherits SqlPackageBase." );
            }
            Debug.Assert( stObj.ItemKind == DependentItemKindSpec.Container, "Since it is a SqlPackageBase." );
            Debug.Assert( item is IMutableSetupItemContainer );

            SqlPackageBaseItem packageItem = (SqlPackageBaseItem)item;

            foreach( var n in Attribute.CommaSeparatedObjectNames.Split( ',' ) )
            {
                string nTrimmed = n.Trim();
                if( nTrimmed.Length > 0 )
                {
                    var protoObject = LoadProtoItemFromResource( logger, packageItem, nTrimmed );
                    if( protoObject != null )
                    {
                        SqlObjectItem subItem = protoObject.CreateItem( logger );
                        ((IMutableSetupItemContainer)item).Children.Add( subItem );
                    }
                }
            }
        }

        static internal SqlObjectProtoItem LoadProtoItemFromResource( IActivityLogger logger, SqlPackageBaseItem packageItem, string objectName )
        {
            SqlPackageBase package = packageItem.Object;
            int schemaDot = objectName.IndexOf( '.' );
            string externalSchema = package.Schema;
            string fileName = objectName + ".sql";
            string text = packageItem.ResourceLocation.GetString( fileName, false );
            if( text == null )
            {
                string failed = fileName;
                // If the objectName does not contains a schema, tries the "packageSchema.objectName.sql".
                if( schemaDot < 0 )
                {
                    fileName = externalSchema + '.' + fileName;
                }
                else
                {
                    // The objectName contains a schema: tries to find the resource without the schema prefix.
                    externalSchema = objectName.Substring( 0, schemaDot );
                    objectName = objectName.Substring( schemaDot + 1 );
                    fileName = objectName + ".sql";
                }
                text = packageItem.ResourceLocation.GetString( fileName, false );
                if( text == null )
                {
                    logger.Error( "Resource for '{0}' not found (tried '{1}' and '{2}').", objectName, fileName, failed );
                    return null;
                }
            }
            else if( schemaDot > 0 )
            {
                externalSchema = objectName.Substring( 0, schemaDot );
                objectName = objectName.Substring( schemaDot + 1 );
            }

            SqlObjectProtoItem protoObject = SqlObjectParser.Create( logger, packageItem, text );
            if( protoObject != null )
            {
                if( protoObject.ObjectName != objectName )
                {
                    logger.Error( "Resource '{0}' contains the definition of '{1}'. Names must match.", fileName, protoObject.Name );
                    protoObject = null;
                }
                else if( protoObject.Schema.Length > 0 && protoObject.Schema != externalSchema )
                {
                    logger.Error( "Resource '{0}' defines the {1} in the schema '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.Schema, externalSchema );
                    protoObject = null;
                }
            }
            return protoObject;
        }
    }
}
