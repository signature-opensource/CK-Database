using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlObjectItemAttribute : Attribute, IStObjSetupDynamicInitializer
    {
        public SqlObjectItemAttribute( string objectName )
        {
            ObjectName = objectName;
        }

        public string ObjectName { get; private set; }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IActivityLogger logger, IMutableSetupItem item, IStObj stObj )
        {
            if( !(stObj.Object is SqlPackageBase) )
            {
                throw new NotSupportedException( "SqlObjectItemAttribute must be set only on class that inherits SqlPackageBase." );
            }
            Debug.Assert( stObj.ItemKind == DependentItemKind.Container, "Since it is a SqlPackageBase." );
            Debug.Assert( item is IMutableSetupItemContainer );

            SqlPackageBase p = (SqlPackageBase)stObj.Object;
            string fileName = ObjectName + ".sql";
            string text = p.ResourceLocation.GetString( fileName, true );
            SqlObjectProtoItem protoObject = SqlObjectParser.Create( logger, item, text );
            if( protoObject != null )
            {
                if( protoObject.ObjectName != ObjectName )
                {
                    logger.Error( "Resource '{0}' contains the definition of '{1}'. Names must match.", fileName, protoObject.Name );
                }
                else if( protoObject.Schema.Length > 0 && protoObject.Schema != p.Schema )
                {
                    logger.Error( "Resource '{0}' defines the {1} in the schema '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.Schema, p.Schema );
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
