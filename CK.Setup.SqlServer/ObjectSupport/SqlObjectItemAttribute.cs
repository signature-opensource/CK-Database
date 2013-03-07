using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{
    /// <summary>
    /// Declares a resource that contains a Sql procedure, function or view associated to a type.
    /// Multiples object names like "sUserCreate, sUserDestroy, sUserUpgrade" can be defined.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlObjectItemAttribute : Attribute, IStObjSetupDynamicInitializer
    {
        /// <summary>
        /// Initializes a new <see cref="SqlObjectItemAttribute"/> with (potentially) multiple object names.
        /// </summary>
        /// <param name="commaSeparatedObjectNames">Name or multiple comma separated names.</param>
        public SqlObjectItemAttribute( string commaSeparatedObjectNames )
        {
            CommaSeparatedObjectNames = commaSeparatedObjectNames;
        }

        /// <summary>
        /// Gets a Sql object name or multiple comma separated names.
        /// </summary>
        public string CommaSeparatedObjectNames { get; private set; }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IActivityLogger logger, IMutableSetupItem item, IStObj stObj )
        {
            if( !(stObj.Object is SqlPackageBase) )
            {
                throw new NotSupportedException( "SqlObjectItemAttribute must be set only on class that inherits SqlPackageBase." );
            }
            Debug.Assert( stObj.ItemKind == DependentItemKind.Container, "Since it is a SqlPackageBase." );
            Debug.Assert( item is IMutableSetupItemContainer );

            SqlPackageBaseItem p = (SqlPackageBaseItem)item;
            SqlPackageBase obj = (SqlPackageBase)stObj.Object;

            foreach( var n in CommaSeparatedObjectNames.Split( ',' ) )
            {
                string nTrimmed = n.Trim();
                if( nTrimmed.Length > 0 )
                {
                    AddChildObjectFromResource( logger, item, p, obj, nTrimmed );
                }
            }
        }

        private void AddChildObjectFromResource( IActivityLogger logger, IMutableSetupItem item, SqlPackageBaseItem p, SqlPackageBase obj, string objectName )
        {
            int schemaDot = objectName.IndexOf( '.' );
            string externalSchema = obj.Schema;
            string fileName = objectName + ".sql";
            string text = p.ResourceLocation.GetString( fileName, false );
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
                text = p.ResourceLocation.GetString( fileName, false );
                if( text == null )
                {
                    logger.Error( "Resource for '{0}' not found (tried '{1}' and '{2}').", objectName, fileName, failed );
                    return;
                }
            }
            else if( schemaDot > 0 )
            {
                externalSchema = objectName.Substring( 0, schemaDot );
                objectName = objectName.Substring( schemaDot + 1 );
            }

            SqlObjectProtoItem protoObject = SqlObjectParser.Create( logger, item, text );
            if( protoObject != null )
            {
                if( protoObject.ObjectName != objectName )
                {
                    logger.Error( "Resource '{0}' contains the definition of '{1}'. Names must match.", fileName, protoObject.Name );
                }
                else if( protoObject.Schema.Length > 0 && protoObject.Schema != externalSchema )
                {
                    logger.Error( "Resource '{0}' defines the {1} in the schema '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.Schema, externalSchema );
                }
                else
                {
                    SqlObjectItem subItem = protoObject.CreateItem( logger );
                    ((IMutableSetupItemContainer)item).Children.Add( subItem );
                }
            }
        }
    }
}
