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
    /// Multiples object names like "sUserCreate, sUserDestroy, sUserUpgrade" may be defined.
    /// </summary>
    public class SqlObjectItemAttributeImpl : SetupObjectItemAttributeImplBase
    {
        public SqlObjectItemAttributeImpl( SqlObjectItemAttribute a )
            : base( a )
        {
        }

        protected new SqlObjectItemAttribute Attribute => (SqlObjectItemAttribute)base.Attribute;

        /// <summary>
        /// Must build the full name of the object based on the raw attribute name, whether this is
        /// a definition, a replacement or a transformation and the container of the object provided 
        /// by the <paramref name="r"/> object.
        /// </summary>
        /// <param name="r">The registerer.</param>
        /// <param name="b">The behavior (Define, Replace or Transform).</param>
        /// <param name="attributeName">The raw attribute name.</param>
        /// <returns>The context-location-name for the object.</returns>
        protected override IContextLocNaming BuildFullName( SetupObjectItemAttributeRegisterer r, SetupObjectItemBehavior b, string attributeName )
        {
            return r.SqlBuildFullName( b, attributeName );
        }

        /// <summary>
        /// When overridden, can return a non null list of item type names.
        /// Item types can not be null nor longer than 16 characters. For Sql Server, this can be
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
            ISqlSetupAspect sql = SetupEngineAspectProvider.GetSetupEngineAspect<ISqlSetupAspect>();
            return SqlBaseItem.Create( 
                sql.SqlParser, 
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
