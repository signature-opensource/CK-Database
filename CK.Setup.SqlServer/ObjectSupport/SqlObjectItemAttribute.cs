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
                if( !String.IsNullOrWhiteSpace( n ) )
                {
                    AddChildObjectFromResource( logger, item, p, obj, n );
                }
            }
        }

        private void AddChildObjectFromResource( IActivityLogger logger, IMutableSetupItem item, SqlPackageBaseItem p, SqlPackageBase obj, string objectName )
        {
            string fileName = objectName + ".sql";
            string text = p.ResourceLocation.GetString( fileName, true );
            SqlObjectProtoItem protoObject = SqlObjectParser.Create( logger, item, text );
            if( protoObject != null )
            {
                if( protoObject.ObjectName != CommaSeparatedObjectNames )
                {
                    logger.Error( "Resource '{0}' contains the definition of '{1}'. Names must match.", fileName, protoObject.Name );
                }
                else if( protoObject.Schema.Length > 0 && protoObject.Schema != obj.Schema )
                {
                    logger.Error( "Resource '{0}' defines the {1} in the schema '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.Schema, obj.Schema );
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
