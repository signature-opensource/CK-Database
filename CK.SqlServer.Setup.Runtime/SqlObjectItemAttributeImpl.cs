#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlObjectItemAttributeImpl.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            if( !(stObj.InitialObject is SqlPackageBase) )
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
                        string[] names = BuildNames( packageItem.GetObject(), nTrimmed );
                        if( names == null )
                        {
                            state.Monitor.Error().Send( "Invalid object name '{0}' in SqlObjectItem attribute of '{1}'.", nTrimmed, item.FullName );
                        }
                        else
                        {
                            var protoObject = LoadProtoItemFromResource( state.Monitor, packageItem, names );
                            if( protoObject != null )
                            {
                                SqlObjectItem subItem = protoObject.CreateItem( state.Monitor );
                                if( subItem != null )
                                {
                                    if( !subItem.MissingDependencyIsError.HasValue ) subItem.MissingDependencyIsError = Attribute.MissingDependencyIsError;
                                    packageItem.Children.Add( subItem );
                                }
                            }
                        }
                    }
                    else state.Monitor.Warn().Send( "Duplicate name '{0}' in SqlObjectItem attribute of '{1}'.", nTrimmed, item.FullName );
                }
            }
        }

        /// <summary>
        /// Builds an array of object names [schema.name, schema, name].
        /// </summary>
        /// <param name="package">Sql package that declares the object.</param>
        /// <param name="objectName">Object name. May contain a schema or not.</param>
        /// <returns>The first one is always of the form 'schema.object'. It is the exact one. Null on invalid names.</returns>
        static internal string[] BuildNames( SqlPackageBase package, string objectName )
        {
            int schemaDot = objectName.IndexOf( '.' );
            if( schemaDot == 0 || schemaDot == objectName.Length-1 ) return null;
            if( schemaDot > 0 )
            {
                return new[]{ 
                    objectName, // schema.obj
                    objectName.Substring( 0, schemaDot ), // schema
                    objectName.Substring( schemaDot + 1 ) // obj 
                }; 
            }
            else 
            {
                 return new[]{ 
                     package.Schema + '.' + objectName, // schema.obj
                     package.Schema, // schema
                     objectName // obj 
                 };
            }
        }

        static internal SqlObjectProtoItem LoadProtoItemFromResource( IActivityMonitor monitor, SqlPackageBaseItem packageItem, string[] names, string expectedItemType = null )
        {
            string fileName = names[0] + ".sql";
            string text = packageItem.ResourceLocation.GetString( fileName, false );
            if( text == null )
            {
                fileName = names[2] + ".sql";
                text = packageItem.ResourceLocation.GetString( fileName, false );
            }
            if( text == null )
            {
                monitor.Error().Send( "Resource '{0}' of '{1}' not found (tried '{2}' and '{3}').", names[0], packageItem.FullName , names[0] + ".sql", fileName );
                return null;
            }

            SqlObjectProtoItem protoObject = SqlObjectParser.Create( monitor, packageItem, text );
            if( protoObject != null )
            {
                if( expectedItemType != null  && protoObject.ItemType != expectedItemType )
                {
                    monitor.Error().Send( "Resource '{0}' of '{1}' is a '{2}' whereas a '{3}' is expected.", fileName, packageItem.FullName, protoObject.ItemType, expectedItemType );
                    protoObject = null;
                }
                else if( protoObject.ObjectName != names[2] )
                {
                    monitor.Error().Send( "Resource '{0}' of '{2}' contains the definition of '{1}'. Names must match.", fileName, protoObject.Name, packageItem.FullName );
                    protoObject = null;
                }
                else if( protoObject.Schema.Length > 0 && protoObject.Schema != names[1] )
                {
                    monitor.Error().Send( "Resource '{0}' of '{4}' defines the {1} in the schema '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.Schema, names[1], packageItem.FullName );
                    protoObject = null;
                }
                else monitor.Trace().Send( "Loaded {0} '{1}' of '{2}'.", protoObject.ItemType, protoObject.Name, packageItem.FullName );
            }
            return protoObject;
        }
    }
}
