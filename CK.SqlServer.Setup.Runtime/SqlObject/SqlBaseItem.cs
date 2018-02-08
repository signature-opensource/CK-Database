using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using CK.Core;
using CK.Setup;
using System.Text;
using CK.SqlServer.Parser;
using System.Text.RegularExpressions;
using System.Linq;
using CK.Text;
using System.Diagnostics;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Base class for <see cref="SqlObjectItem"/> and <see cref="SqlTransformerItem"/>.
    /// </summary>
    public abstract class SqlBaseItem : SetupObjectItem
    {
        ISqlServerParsedText _sqlObject;
        IReadOnlyList<SqlTransformerItem> _transformers;

        /// <summary>
        /// Initializes a <see cref="SqlBaseItem"/>.
        /// </summary>
        /// <param name="name">The object name.</param>
        /// <param name="itemType">The item type.</param>
        /// <param name="parsed">The parsed text.</param>
        protected SqlBaseItem( SqlContextLocName name, string itemType, ISqlServerParsedText parsed )
            : base( name, itemType )
        {
            _sqlObject = parsed;
        }

        /// <summary>
        /// Gets the transform target item if this item has associated <see cref="Transformers"/>.
        /// This object is created as a clone of this object by the first call 
        /// to this <see cref="SetupObjectItem.AddTransformer"/> method.
        /// </summary>
        public new SqlBaseItem TransformTarget => (SqlBaseItem)base.TransformTarget;

        /// <summary>
        /// Gets the source item if this item is a target, null otherwise.
        /// </summary>
        public new SqlBaseItem TransformSource => (SqlBaseItem)base.TransformSource;

        /// <summary>
        /// Gets the transformers that have been registered with <see cref="SetupObjectItem.AddTransformer">AddTransformer</see>.
        /// Never null (empty when no transformers have been added yet).
        /// </summary>
        public new IReadOnlyList<SqlTransformerItem> Transformers => _transformers ?? (_transformers = CreateTypedTransformersWrapper<SqlTransformerItem>());

        /// <summary>
        /// Gets the <see cref="SqlContextLocName"/> name of this object.
        /// </summary>
        public new SqlContextLocName ContextLocName => (SqlContextLocName)base.ContextLocName; 

        /// <summary>
        /// Gets or sets the <see cref="ISqlServerParsedText"/> associated object.
        /// </summary>
        public ISqlServerParsedText SqlObject
        {
            get { return _sqlObject; }
            set { _sqlObject = value; }
        }

        /// <summary>
        /// Initializes this item after its instanciation.
        /// This default implementation reads the <see cref="SqlObject"/> header (the comments)
        /// and uses <see cref="CreateConfigReader"/> to apply it.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="firstContainer">
        /// The first container that defined this object: it is different than the <paramref name="packageItem"/>
        /// if it is a replacement.
        /// On success, this will be the package of the item if the item does not specify a container.
        /// </param>
        /// <param name="packageItem">
        /// The package that defined the item.
        /// </param>
        /// <returns>True on success, false on error.</returns>
        protected virtual bool Initialize( IActivityMonitor monitor, IDependentItemContainer firstContainer, IDependentItemContainer packageItem )
        {
            bool foundConfig;
            string h = SqlObject.HeaderComments.Select( c => c.Text ).Concatenate( Environment.NewLine );
            var configReader = CreateConfigReader();
            if( !configReader.Apply( monitor, h, out foundConfig ) ) return false;
            if( !foundConfig )
            {
                monitor.Warn( "Missing SetupConfig:{}. At least an empty one should appear in the header." );
            }
            return true;
        }

        /// <summary>
        /// Extension point that enables to substitute the default <see cref="SetupConfigReader"/> used 
        /// to initialize this object.
        /// </summary>
        /// <returns>The configuration reader to use.</returns>
        public virtual SetupConfigReader CreateConfigReader() => new SetupConfigReader( this );

        /// <summary>
        /// Factory method that handles resource loading (based on name and containing package of the object),
        /// parsing of the resource text and creation of a <see cref="SqlBaseItem"/> either from an optional 
        /// factory method or based on the resource text content and its initialization thanks to <see cref="Initialize"/>.
        /// </summary>
        /// <param name="parser">The Sql parser to use.</param>
        /// <param name="r">The registerer that gives access to the <see cref="IStObjSetupDynamicInitializerState"/>.</param>
        /// <param name="name">Full name of the object to create.</param>
        /// <param name="firstContainer">
        /// The first container that defined this object.
        /// Actual container if the object has been replaced is provided by 
        /// <see cref="SetupObjectItemAttributeImplBase.Registerer">Registerer</see>.Container.
        /// </param>
        /// <param name="transformArgument">Optional transform argument if this object is a transformer.</param>
        /// <param name="expectedItemTypes">Optional expected item types (can be null).</param>
        /// <param name="factory">
        /// Factory function for result. When null, standard items (views, functions, etc.) are
        /// created based on the actual resource text.
        /// </param>
        /// <returns>The created object or null if an error occurred and has been logged.</returns>
        public static SqlBaseItem CreateStandardSqlBaseItem(
                ISqlServerParser parser,
                SetupObjectItemAttributeRegisterer r,
                SqlContextLocName name,
                SqlPackageBaseItem firstContainer,
                SqlBaseItem transformArgument,
                IEnumerable<string> expectedItemTypes,
                Func<SetupObjectItemAttributeRegisterer, SqlContextLocName, ISqlServerParsedText, SqlBaseItem> factory = null )
        {
            Debug.Assert( (transformArgument != null) == (name.TransformArg != null) );
            SqlPackageBaseItem packageItem = (SqlPackageBaseItem)r.Container;
            using( r.Monitor.OpenTrace( $"Loading '{name}' of '{r.Container.FullName}'." ) )
            {
                string fileName;
                string text = name.LoadTextResource( r.Monitor, packageItem, out fileName );
                if( text == null ) return null;
                SqlBaseItem result = ParseAndInitialize( r, name, parser, text, firstContainer, packageItem, transformArgument, expectedItemTypes, factory );
                if( result != null )
                {
                    if( result.Container == null ) firstContainer.Children.Add( result );
                    r.Monitor.CloseGroup( $"Loaded {result.ItemType} from file '{fileName}'." );
                }
                else r.Monitor.CloseGroup( $"Error while loading file '{fileName}'." );
                return result;
            }
        }

        static SqlBaseItem ParseAndInitialize(
            SetupObjectItemAttributeRegisterer registerer,
            SqlContextLocName name,
            ISqlServerParser parser,
            string text,
            IDependentItemContainer firstContainer,
            IDependentItemContainer packageItem,
            SqlBaseItem transformArgument,
            IEnumerable<string> expectedItemTypes,
            Func<SetupObjectItemAttributeRegisterer, SqlContextLocName, ISqlServerParsedText, SqlBaseItem> factory = null )
        {
            try
            {
                var r = parser.Parse( text );
                if( r.IsError )
                {
                    r.LogOnError( registerer.Monitor );
                    return null;
                }
                if( transformArgument != null ) expectedItemTypes = new[] { "Transformer" };
                ISqlServerParsedText oText = r.Result;
                bool factoryError = false;
                SqlBaseItem result = null;
                using( registerer.Monitor.OnError( () => factoryError = true ))
                {
                    result = factory( registerer, name, oText );
                }
                if( result == null )
                {
                    if( factoryError ) return null;
                    result = DefaultFactory( name, oText );
                }
                if( expectedItemTypes != null && !expectedItemTypes.Contains( result.ItemType ) )
                {
                    registerer.Monitor.Error( $"Content is a '{result.ItemType}' whereas '{expectedItemTypes.Concatenate( "' or '" )}' is expected." );
                    return null;
                }
                SqlTransformerItem t = result as SqlTransformerItem;
                if( t != null )
                {
                    if( transformArgument.AddTransformer( registerer.Monitor, t ) == null ) return null;
                }
                return result.Initialize( registerer.Monitor, firstContainer, packageItem ) ? result : null;
            }
            catch( Exception ex )
            {
                using( registerer.Monitor.OpenError( ex ) )
                {
                    registerer.Monitor.Info( text );
                }
                return null;
            }
        }

        /// <summary>
        /// Factory for <see cref="SqlBaseItem"/>.
        /// </summary>
        /// <param name="name">The object name.</param>
        /// <param name="oText">The parsed text.</param>
        /// <returns>A Sql item.</returns>
        static SqlBaseItem DefaultFactory( SqlContextLocName name, ISqlServerParsedText oText )
        {
            SqlBaseItem result = null;
            if( oText is ISqlServerObject )
            {
                if( oText is ISqlServerCallableObject )
                {
                    if( oText is ISqlServerStoredProcedure )
                    {
                        result = new SqlProcedureItem( name, (ISqlServerStoredProcedure)oText );
                    }
                    else if( oText is ISqlServerFunctionScalar )
                    {
                        result = new SqlFunctionScalarItem( name, (ISqlServerFunctionScalar)oText );
                    }
                    else if( oText is ISqlServerFunctionInlineTable )
                    {
                        result = new SqlFunctionInlineTableItem( name, (ISqlServerFunctionInlineTable)oText );
                    }
                    else if( oText is ISqlServerFunctionTable )
                    {
                        result = new SqlFunctionTableItem( name, (ISqlServerFunctionTable)oText );
                    }
                }
                else if( oText is ISqlServerView )
                {
                    result = new SqlViewItem( name, (ISqlServerView)oText );
                }
            }
            else if( oText is ISqlServerTransformer )
            {
                result = new SqlTransformerItem( name, (ISqlServerTransformer)oText );
            }
            if( result == null )
            {
                throw new NotSupportedException( "Unhandled type of object: " + oText.ToString() );
            }
            return result;
        }
    }
}
