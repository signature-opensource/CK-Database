using System;
using System.Collections.Generic;
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

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjRuntime stObj )
        {
            if( !(stObj.Object is SqlPackageBase) )
            {
                throw new NotSupportedException( "SqlObjectItemAttribute must be set only on class that inherits SqlPackageBase." );
            }
            Debug.Assert( stObj.ItemKind == DependentItemKindSpec.Container, "Since it is a SqlPackageBase." );
            Debug.Assert( item is IMutableSetupItemContainer );

            SqlPackageBaseItem packageItem = (SqlPackageBaseItem)item;

            HashSet<string> already = new HashSet<string>();
            foreach( var n in Attribute.CommaSeparatedObjectNames.Split( ',' ) )
            {
                string nTrimmed = n.Trim();
                if( nTrimmed.Length > 0 )
                {
                    if( already.Add( nTrimmed ) )
                    {
                        var protoObject = LoadProtoItemFromResource( state.Logger, packageItem, nTrimmed );
                        if( protoObject != null )
                        {
                            SqlObjectItem subItem = protoObject.CreateItem( state.Logger );
                            if( subItem != null )
                            {
                                if( !subItem.MissingDependencyIsError.HasValue ) subItem.MissingDependencyIsError = Attribute.MissingDependencyIsError;
                                packageItem.Children.Add( subItem );
                            }
                        }
                    }
                    else state.Logger.Warn( "Duplicate name '{0}' in SqlObjectItem attribute of '{1}'.", nTrimmed, item.FullName );
                }
            }
        }

        static internal SqlObjectProtoItem LoadProtoItemFromResource( IActivityLogger logger, SqlPackageBaseItem packageItem, string objectName, string expectedItemType = null )
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
                    logger.Error( "Resource '{0}' of '{3}' not found (tried '{1}' and '{2}').", objectName, fileName, failed, packageItem.FullName );
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
                if( expectedItemType != null  && protoObject.ItemType != expectedItemType )
                {
                    logger.Error( "Resource '{0}' of '{1}' is a '{2}' whereas a '{3}' is expected.", fileName, packageItem.FullName, protoObject.ItemType, expectedItemType );
                    protoObject = null;
                }
                else if( protoObject.ObjectName != objectName )
                {
                    logger.Error( "Resource '{0}' of '{2}' contains the definition of '{1}'. Names must match.", fileName, protoObject.Name, packageItem.FullName );
                    protoObject = null;
                }
                else if( protoObject.Schema.Length > 0 && protoObject.Schema != externalSchema )
                {
                    logger.Error( "Resource '{0}' of '{4}' defines the {1} in the schema '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.Schema, externalSchema, packageItem.FullName );
                    protoObject = null;
                }
                else logger.Trace( "Loaded {0} '{1}' of '{2}'.", protoObject.ItemType, protoObject.Name, packageItem.FullName );
            }
            return protoObject;
        }
    }
}
