using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using CK.Text;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Declares a resource that contains a Sql procedure, function or view associated to a type.
    /// Multiples object names like "sUserCreate, sUserDestroy, sUserUpgrade" can be defined.
    /// </summary>
    public class SqlObjectItemAttributeImpl : SetupObjectItemAttributeImplBase
    {
        static readonly string[] _allowedResourcePrefixes = new string[] { "[Replace]", "[Transform]" };

        public SqlObjectItemAttributeImpl( SqlObjectItemAttribute a )
            : base( a )
        {
        }

        protected new SqlObjectItemAttribute Attribute => (SqlObjectItemAttribute)base.Attribute;

        protected override IContextLocNaming BuildFullName( Registerer r, SetupObjectItemBehavior b, string attributeName )
        {
            SqlPackageBaseItem p = (SqlPackageBaseItem)r.Container;
            return SqlBuildFullName( p, b, attributeName );
        }

        protected override SetupObjectItem CreateSetupObjectItem( Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name )
        {
            ISqlSetupAspect sql = SetupEngineAspectProvider.GetSetupEngineAspect<ISqlSetupAspect>();
            return SqlCreateSetupObjectItem( sql.SqlParser, r.Monitor, (SqlPackageBaseItem)r.Container, (SqlPackageBaseItem)firstContainer, Attribute.MissingDependencyIsError, (SqlContextLocName)name, null );
        }

        internal static IContextLocNaming SqlBuildFullName( SqlPackageBaseItem p, SetupObjectItemBehavior b, string attributeName )
        {
            var name = new SqlContextLocName( attributeName );
            if( name.Context == null ) name.Context = p.Context;
            if( name.Location == null ) name.Location = p.Location;
            if( name.Schema == null ) name.Schema = p.GetObject().Schema;
            if( name.TransformArg != null )
            {
                var target = new SqlContextLocName( name.TransformArg );
                if( target.Context == null ) target.Context = name.Context;
                if( target.Location == null ) target.Location = name.Location;
                if( target.Schema == null ) target.Schema = name.Schema;
                name.TransformArg = target.FullName;
            }
            else
            {
                if( b == SetupObjectItemBehavior.Transform )
                {
                    name = new SqlContextLocName( p.Context, p.Location, p.Name + '(' + name.FullName + ')' );
                }
            }
            return name;
        }

        static internal SetupObjectItem SqlCreateSetupObjectItem( 
            ISqlServerParser parser, 
            IActivityMonitor monitor,
            SqlPackageBaseItem firstContainer,
            SqlPackageBaseItem packageItem, 
            bool missingDependencyIsError, 
            SqlContextLocName name, 
            string expectedItemType = null )
        {
            if( name.TransformArg != null ) expectedItemType = SqlObjectProtoItem.TypeTransformer;
            SqlObjectProtoItem protoObject = LoadProtoItemFromResource( monitor, packageItem, name, expectedItemType );
            if( protoObject == null ) return null;
            var item = protoObject.CreateItem( parser, monitor, missingDependencyIsError );
            firstContainer.EnsureObjectsPackage().Children.Add( item );
            return item;
        }

        static SqlObjectProtoItem LoadProtoItemFromResource( IActivityMonitor monitor, SqlPackageBaseItem packageItem, SqlContextLocName name, string expectedItemType )
        {
            var candidates = GetResourceNameCandidates( packageItem, name );
            string fileName = null, text = null;
            foreach( var fName in candidates )
            {
                fileName = fName + ".sql";
                if( (text = packageItem.ResourceLocation.GetString( fileName, false, _allowedResourcePrefixes )) != null ) break;
            }
            if( text == null )
            {
                monitor.Error().Send( $"Resource '{name.FullName}' of '{packageItem.FullName}' not found. Tried: '{candidates.Concatenate( ".sql' ,'" )}.sql'." );
                return null;
            }

            SqlObjectProtoItem protoObject = SqlObjectParser.Create( monitor, packageItem, text );
            if( protoObject != null )
            {
                if( expectedItemType != null && protoObject.ItemType != expectedItemType )
                {
                    monitor.Error().Send( "Resource '{0}' of '{1}' is a '{2}' whereas a '{3}' is expected.", fileName, packageItem.FullName, protoObject.ItemType, expectedItemType );
                    return null;
                }
                if( !protoObject.IsTransformer || protoObject.ContextLocName.ObjectName != string.Empty )
                {
                    if( protoObject.ContextLocName.ObjectName != name.ObjectName )
                    {
                        monitor.Error().Send( "Resource '{0}' of '{2}' contains the definition of '{1}'. Names must match.", fileName, protoObject.ContextLocName.Name, packageItem.FullName );
                        return null;
                    }
                    if( string.IsNullOrEmpty( protoObject.ContextLocName.Schema ) )
                    {
                        protoObject.ContextLocName.Schema = name.Schema;
                        monitor.Trace().Send( "{0} '{1}' does not specify a schema: it will use '{2}' schema.", protoObject.ItemType, protoObject.ContextLocName.Name, name.Schema );
                    }
                    else if( protoObject.ContextLocName.Schema != name.Schema )
                    {
                        monitor.Error().Send( "Resource '{0}' of '{4}' defines the {1} in the schema '{2}' instead of '{3}'.", fileName, protoObject.ItemType, protoObject.ContextLocName.Schema, name.Schema, packageItem.FullName );
                        return null;
                    }
                    if( protoObject.IsTransformer )
                    {
                        // Named transformer and its name matches the referenced name.
                    }
                }
                else
                {
                    Debug.Assert( protoObject.IsTransformer && protoObject.ContextLocName.ObjectName == string.Empty );
                    protoObject.ContextLocName.Name = name.Name;
                }
            }
            if( protoObject != null ) monitor.Trace().Send( "Loaded {0} '{1}' of '{2}'.", protoObject.ItemType, protoObject.ContextLocName.Name, packageItem.FullName );
            return protoObject;
        }

        static IEnumerable<string> GetResourceNameCandidates( IContextLocNaming containerName, SqlContextLocName name )
        {
            yield return name.FullName;
            yield return name.Name;
            yield return name.ObjectName;
            if( name.TransformArg != null )
            {
                if( name.FullName.StartsWith( containerName.FullName ) )
                {
                    SqlContextLocName t = new SqlContextLocName( name.TransformArg );
                    yield return t.FullName;
                    yield return t.Name;
                    yield return t.ObjectName;
                }
            }
        }

    }

}
