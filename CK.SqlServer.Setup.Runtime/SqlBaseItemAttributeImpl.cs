using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using System.Collections.Generic;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Implementation for <see cref="SqlObjectItemAttribute"/> or other specialization of <see cref="SetupObjectItemAttributeImplBase"/>.
    /// Any kind of <see cref="SqlBaseItem"/> can be handled by this base class thanks to the 
    /// overridable <see cref="CreateSqlBaseItem"/> factory method.
    /// </summary>
    public class SqlBaseItemAttributeImpl : SetupObjectItemAttributeImplBase
    {
        readonly ISqlServerParser _parser;

        public SqlBaseItemAttributeImpl( SetupObjectItemAttributeBase a, ISqlServerParser parser )
            : base( a )
        {
            _parser = parser;
        }

        /// <summary>
        /// Must build the full name of the item based on the raw attribute name, whether this is
        /// a definition, a replacement or a transformation and the container of the item.
        /// </summary>
        /// <param name="container">The item's container.</param>
        /// <param name="b">The behavior (Define, Replace or Transform).</param>
        /// <param name="attributeName">The raw attribute name.</param>
        /// <returns>The context-location-name for the item.</returns>
        protected override IContextLocNaming BuildFullName( ISetupItem container, SetupObjectItemBehavior b, string attributeName )
        {
            return SqlBuildFullName( (SqlPackageBaseItem)container, b, attributeName );
        }

        /// <summary>
        /// Builds a Sql context-location-name (with the <see cref="SqlContextLocName.Schema"/>) from a setup object 
        /// name (typically from an attribute) and its <see cref="SqlPackageBaseItem"/> container that provides
        /// ambient context, location and schema if the <paramref name="attributeName"/> does not define them.
        /// When the behavior is <see cref="SetupObjectItemBehavior.Transform"/> and the name does not have
        /// a transform argument, we consider it to be the default transformation of the (target) name by the container.
        /// </summary>
        /// <param name="container">The item's container.</param>
        /// <param name="b">The behavior (define, replace or transform).</param>
        /// <param name="attributeName">Name of the object defined in the attribute.</param>
        /// <returns>The Sql context-location-name.</returns>
        public static SqlContextLocName SqlBuildFullName( SqlPackageBaseItem container, SetupObjectItemBehavior b, string attributeName )
        {
            var name = new SqlContextLocName( attributeName );
            if( name.Context == null ) name.Context = container.Context;
            if( name.Location == null ) name.Location = container.Location;
            if( name.Schema == null ) name.Schema = container.ActualObject.Schema;
            // Now handling transformation.
            if( name.TransformArg != null )
            {
                // The provided name is a transformation: resolves context/location/schema from container 
                // on the target component if they are not define.
                var target = new SqlContextLocName( name.TransformArg );
                if( target.Context == null ) target.Context = name.Context;
                if( target.Location == null ) target.Location = name.Location;
                if( target.Schema == null ) target.Schema = name.Schema;
                name.TransformArg = target.FullName;
            }
            else if( b == SetupObjectItemBehavior.Transform )
            {
                // The name is not the name of a transformation however it should be:
                // we consider it to be the default transformation of the (target) name by the container.
                name = new SqlContextLocName( container.Context, container.Location, container.Name + '(' + name.FullName + ')' );
            }
            return name;
        }

        /// <summary>
        /// When overridden, can return a non null list of item type names.
        /// Item types can not be null nor longer than 16 characters. For Sql Server, standard types are
        /// "Function" (covers ITVF, table and scalar function), "Procedure", "View" and "Transformer".
        /// </summary>
        protected virtual IEnumerable<string> ExpectedItemTypes => null;

        /// <summary>
        /// Creates the <see cref="SetupObjectItem"/>.
        /// This is called only once the potential replacements have been analysed and resolved.
        /// </summary>
        /// <param name="r">The registerer that gives access to the <see cref="IStObjSetupDynamicInitializerState"/>.</param>
        /// <param name="firstContainer">
        /// The first container that defined this object.
        /// Actual container if the object has been replaced is provided by 
        /// <see cref="SetupObjectItemAttributeImplBase.Registerer">Registerer</see>.Container.
        /// </param>
        /// <param name="name">Full name of the object to create.</param>
        /// <param name="transformArgument">Optional transform argument if this object is a transformer.</param>
        /// <returns>The created object or null if an error occurred and has been logged.</returns>
        protected override SetupObjectItem CreateSetupObjectItem( SetupObjectItemAttributeRegisterer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument )
        {
            return SqlBaseItem.Create(
                _parser, 
                r, 
                (SqlContextLocName)name, 
                (SqlPackageBaseItem)firstContainer, 
                (SqlBaseItem)transformArgument, 
                ExpectedItemTypes,
                CreateSqlBaseItem );
        }

        /// <summary>
        /// Extension point to create specialized <see cref="SqlBaseItem"/> (other than the standard objects like <see cref="SqlViewItem"/>,
        /// or <see cref="SqlProcedure"/>).
        /// Returns null by default: returning null triggers the use of a default factory that handles the standard items.
        /// This can also be used to inspect/validate the <paramref name="text"/> since error or fatal logged to the <paramref name="monitor"/> stops the process.
        /// </summary>
        /// <param name="r">The registerer that gives access to the <see cref="IStObjSetupDynamicInitializerState"/>.</param>
        /// <param name="name">The item name.</param>
        /// <param name="text">The parsed text.</param>
        /// <returns>A new <see cref="SqlBaseItem"/> or null (if an error occured or the default factory must be used).</returns>
        protected virtual SqlBaseItem CreateSqlBaseItem( SetupObjectItemAttributeRegisterer r, SqlContextLocName name, ISqlServerParsedText text )
        {
            return null;
        }
    }

}
