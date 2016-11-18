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

        public new SqlBaseItem TransformTarget => (SqlBaseItem)base.TransformTarget;

        public new SqlBaseItem TransformSource => (SqlBaseItem)base.TransformSource;

        public new IReadOnlyList<SqlTransformerItem> Transformers => _transformers ?? (_transformers = CreateTypedTransformersWrapper<SqlTransformerItem>());

        public new SqlContextLocName ContextLocName => (SqlContextLocName)base.ContextLocName; 

        /// <summary>
        /// Gets or sets the <see cref="ISqlServerParsedText"/> associated object.
        /// </summary>
        public ISqlServerParsedText SqlObject
        {
            get { return _sqlObject; }
            set { _sqlObject = value; }
        }

        internal abstract bool Initialize( IActivityMonitor monitor, string fileName, IDependentItemContainer packageItem );

        /// <summary>
        /// Extension point that enables to substitute the default <see cref="SetupConfigReader"/> used 
        /// to initialize this object.
        /// </summary>
        /// <returns>The configuration reader to use.</returns>
        internal protected abstract SetupConfigReader CreateConfigReader();

        internal static SqlBaseItem Parse(
            SetupObjectItemAttributeRegisterer registerer,
            SqlContextLocName name,
            ISqlServerParser parser,
            string text,
            string fileName,
            IDependentItemContainer packageItem,
            SqlBaseItem transformArgument,
            IEnumerable<string> expectedItemTypes,
            Func<SetupObjectItemAttributeRegisterer, SqlContextLocName, ISqlServerParsedText,SqlBaseItem> factory = null )
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
                    registerer.Monitor.Error().Send( $"Resource '{fileName}' of '{packageItem?.FullName}' is a '{result.ItemType}' whereas '{expectedItemTypes.Concatenate( "' or '" )}' is expected." );
                    return null;
                }
                SqlTransformerItem t = result as SqlTransformerItem;
                if( t != null )
                {
                    if( transformArgument.AddTransformer( registerer.Monitor, t ) == null ) return null;
                }
                return result.Initialize( registerer.Monitor, fileName, packageItem ) ? result : null;
            }
            catch( Exception ex )
            {
                using( registerer.Monitor.OpenError().Send( ex, $"While parsing '{fileName}'." ) )
                {
                    registerer.Monitor.Info().Send( text );
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
