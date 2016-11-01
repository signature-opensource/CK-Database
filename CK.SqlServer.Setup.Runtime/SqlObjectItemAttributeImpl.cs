using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using CK.Text;
using Yodii.Script;

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

        /// <summary>
        /// When overridden, can return a non null list of item type names.
        /// Item types can not be null nor longer than 16 characters. For Sql Server, this can be
        /// "Function" (covers ITVF, table and scalar function), "Procedure", "View" and "Transformer".
        /// </summary>
        protected virtual IEnumerable<string> ExpectedItemTypes => null;

        protected override SetupObjectItem CreateSetupObjectItem( Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument )
        {
            ISqlSetupAspect sql = SetupEngineAspectProvider.GetSetupEngineAspect<ISqlSetupAspect>();
            return SqlCreateSetupObjectItem( sql.SqlParser, r.Monitor, (SqlPackageBaseItem)r.Container, Attribute.MissingDependencyIsError, (SqlContextLocName)name, (SqlPackageBaseItem)firstContainer, (SqlBaseItem)transformArgument, ExpectedItemTypes );
        }

        internal static IContextLocNaming SqlBuildFullName( SqlPackageBaseItem p, SetupObjectItemBehavior b, string attributeName )
        {
            var name = new SqlContextLocName( attributeName );
            if( name.Context == null ) name.Context = p.Context;
            if( name.Location == null ) name.Location = p.Location;
            if( name.Schema == null ) name.Schema = p.ActualObject.Schema;
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
                SqlPackageBaseItem packageItem,
                bool missingDependencyIsError,
                SqlContextLocName name,
                SqlPackageBaseItem firstContainer,
                SqlBaseItem transformArgument,
                IEnumerable<string> expectedItemTypes )
        {
            Debug.Assert( (transformArgument != null) == (name.TransformArg != null) );
            string fileName;
            string text = LoadTextResource( monitor, packageItem, name, out fileName );
            if( text == null ) return null;
            SqlBaseItem result = SqlBaseItem.Parse( monitor, name, parser, text, fileName, packageItem, transformArgument, expectedItemTypes );
            if( result == null ) return null;
            firstContainer.Children.Add( result );
            monitor.Trace().Send( $"Loaded {result.ItemType} '{result.ContextLocName.Name}' of '{packageItem.FullName}'." );
            return result;
        }

        static string LoadTextResource( IActivityMonitor monitor, SqlPackageBaseItem packageItem, SqlContextLocName name, out string fileName )
        {
            var candidates = GetResourceFileNameCandidates( packageItem, name );
            fileName = null;
            string text = null;
            foreach( var fName in candidates )
            {
                fileName = fName;
                if( (text = packageItem.ResourceLocation.GetString( fileName, false, _allowedResourcePrefixes )) != null ) break;
            }
            if( text == null )
            {
                monitor.Error().Send( $"Resource '{name.FullName}' of '{packageItem.FullName}' not found. Tried: '{candidates.Concatenate( "' ,'" )}'." );
                return null;
            }
            if( fileName.EndsWith( ".y4" ) )
            {
                text = SqlPackageBaseItem.ProcessY4Template( monitor, null, packageItem, null, fileName, text );
            }
            return text;
        }

        static IEnumerable<string> GetResourceFileNameCandidates( IContextLocNaming containerName, SqlContextLocName name )
        {
            bool isTransform = name.TransformArg != null;
            var y4 = GetResourceNameCandidates( containerName, name ).Select( r => r + ".y4" );
            var sql = GetResourceNameCandidates( containerName, name ).Select( r => r + ".sql" );
            if( isTransform )
            {
                var tql = GetResourceNameCandidates( containerName, name ).Select( r => r + ".tql" );
                return tql.Concat( sql ).Concat( y4 );
            }
            return sql.Concat( y4 );
        }

        static IEnumerable<string> GetResourceNameCandidates( IContextLocNaming containerName, SqlContextLocName name )
        {
            yield return name.ObjectName;
            yield return name.Name;
            yield return name.FullName;
            if( name.TransformArg != null )
            {
                if( name.FullName.StartsWith( containerName.FullName ) )
                {
                    SqlContextLocName t = new SqlContextLocName( name.TransformArg );
                    yield return t.ObjectName;
                    yield return t.Name;
                    yield return t.FullName;
                }
            }
        }

    }

}
