using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Declares a resource that contains a Sql procedure, function or view associated to a type.
    /// Multiples object names like "sUserCreate, sUserDestroy, sUserUpgrade" can be defined.
    /// </summary>
    public class SqlObjectItemAttributeImpl : SetupObjectItemAttributeImplBase
    {
        static readonly string[] _allowedResourcePrefixes = new string[] { "[Replace]", "[Override]" };

        public SqlObjectItemAttributeImpl( SqlObjectItemAttribute a )
            : base( a )
        {
        }

        protected new SqlObjectItemAttribute Attribute
        {
            get { return (SqlObjectItemAttribute)base.Attribute; }
        }

        protected override IContextLocNaming BuildFullName( IMutableSetupItem ownerItem, IStObjResult ownerStObj, string attributeName )
        {
            SqlPackageBaseItem p = (SqlPackageBaseItem)ownerItem;
            var name = new SqlContextLocName( attributeName );
            if( name.Context == null ) name.Context = p.Context;
            if( name.Location == null ) name.Location = p.Location;
            if( name.Schema == null ) name.Schema = p.GetObject().Schema;
            return name;
        }

        protected override SetupObjectItem CreateSetupObjectItem( IActivityMonitor monitor, IMutableSetupItem ownerItem, IStObjResult ownerStObj, IContextLocNaming name )
        {
            SqlPackageBaseItem p = (SqlPackageBaseItem)ownerItem;
            ISqlSetupAspect sql = SetupEngineAspectProvider.GetSetupEngineAspect<ISqlSetupAspect>();
            return LoadItemFromResource( sql.SqlParser, monitor, p, Attribute.MissingDependencyIsError, (SqlContextLocName)name, null );
        }

        static internal SqlObjectItem LoadItemFromResource( ISqlServerParser parser, IActivityMonitor monitor, SqlPackageBaseItem packageItem, bool missingDependencyIsError, SqlContextLocName name, string expectedItemType = null )
        {
            SqlObjectProtoItem protoObject = LoadProtoItemFromResource( monitor, packageItem, name, expectedItemType );
            if( protoObject == null ) return null;
            return protoObject.CreateItem( parser, monitor, missingDependencyIsError, packageItem );
        }

        static SqlObjectProtoItem LoadProtoItemFromResource( IActivityMonitor monitor, SqlPackageBaseItem packageItem, SqlContextLocName name, string expectedItemType )
        {
            string fileName = name.Name + ".sql";
            string text = packageItem.ResourceLocation.GetString( fileName, false, _allowedResourcePrefixes );
            if( text == null )
            {
                fileName = name.ObjectName + ".sql";
                text = packageItem.ResourceLocation.GetString( fileName, false, _allowedResourcePrefixes );
            }
            if( text == null )
            {
                monitor.Error().Send( "Resource '{0}' of '{1}' not found (tried '{2}' and '{3}').", name.Name, packageItem.FullName, name.Name + ".sql", fileName );
                return null;
            }

            SqlObjectProtoItem protoObject = SqlObjectParser.Create( monitor, packageItem, text );
            if( protoObject != null )
            {
                if( expectedItemType != null && protoObject.ItemType != expectedItemType )
                {
                    monitor.Error().Send( "Resource '{0}' of '{1}' is a '{2}' whereas a '{3}' is expected.", fileName, packageItem.FullName, protoObject.ItemType, expectedItemType );
                    protoObject = null;
                }
                else if( protoObject.ContextLocName.ObjectName != name.ObjectName )
                {
                    monitor.Error().Send( "Resource '{0}' of '{2}' contains the definition of '{1}'. Names must match.", fileName, protoObject.ContextLocName.Name, packageItem.FullName );
                    protoObject = null;
                }
                else if( String.IsNullOrEmpty( protoObject.ContextLocName.Schema ) )
                {
                    protoObject.ContextLocName.Schema = name.Schema;
                    monitor.Trace().Send( "{0} '{1}' does not specify a schema: it will use '{2}' schema.", protoObject.ItemType, protoObject.ContextLocName.Name, name.Schema );
                }
                else if( protoObject.ContextLocName.Schema.Length > 0 && protoObject.ContextLocName.Schema != name.Schema )
                {
                    monitor.Error().Send( "Resource '{0}' of '{4}' defines the {1} in the schema '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.ContextLocName.Schema, name.Schema, packageItem.FullName );
                    protoObject = null;
                }
            }
            if( protoObject != null ) monitor.Trace().Send( "Loaded {0} '{1}' of '{2}'.", protoObject.ItemType, protoObject.ContextLocName.Name, packageItem.FullName );
            return protoObject;
        }

    }

}
