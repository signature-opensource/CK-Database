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
    public abstract class SqlBaseItem : SetupObjectItemV
    {
        ISqlServerParsedText _sqlObject;
        IReadOnlyList<SqlTransformerItem> _transformers;

        internal SqlBaseItem()
        {
        }

        internal SqlBaseItem( SqlContextLocName name, string itemType, ISqlServerParsedText parsed )
            : base( name, itemType )
        {
            _sqlObject = parsed;
        }

        public new SqlBaseItem TransformTarget => (SqlBaseItem)base.TransformTarget;

        public new SqlBaseItem TransformSource => (SqlBaseItem)base.TransformSource;

        public new IReadOnlyList<SqlTransformerItem> Transformers => _transformers ?? (_transformers = CreateTypedTransformersWrapper<SqlTransformerItem>());

        public new SqlContextLocName ContextLocName
        {
            get { return (SqlContextLocName)base.ContextLocName; }
        }

        /// <summary>
        /// Gets or sets the <see cref="ISqlServerParsedText"/> associated object.
        /// </summary>
        public ISqlServerParsedText SqlObject
        {
            get { return _sqlObject; }
            set { _sqlObject = value; }
        }

        internal abstract bool Initialize( IActivityMonitor monitor, string fileName, IDependentItemContainer packageItem );

        internal static SqlBaseItem Parse(
            IActivityMonitor monitor,
            SqlContextLocName name,
            ISqlServerParser parser,
            string text,
            string fileName,
            IDependentItemContainer packageItem,
            SqlBaseItem transformArgument,
            IEnumerable<string> expectedItemTypes )
        {
            try
            {
                var r = parser.Parse( text );
                if( r.IsError )
                {
                    r.LogOnError( monitor );
                    return null;
                }
                if( transformArgument != null ) expectedItemTypes = new[] { "Transformer" };
                ISqlServerParsedText oText = r.Result;
                SqlTransformerItem t = null;
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
                    result = t = new SqlTransformerItem( name, (ISqlServerTransformer)oText );
                }
                if( result == null )
                {
                    throw new NotSupportedException( "Unhandled type of object: " + oText.ToString() );
                }
                if( expectedItemTypes != null && !expectedItemTypes.Contains( result.ItemType ) )
                {
                    monitor.Error().Send( $"Resource '{fileName}' of '{packageItem?.FullName}' is a '{result.ItemType}' whereas '{expectedItemTypes.Concatenate( "' or '" )}' is expected." );
                    return null;
                }
                if( t != null )
                {
                    if( transformArgument.AddTransformer( monitor, t ) == null ) return null;
                }
                return result.Initialize( monitor, fileName, packageItem ) ? result : null;
            }
            catch( Exception ex )
            {
                using( monitor.OpenError().Send( ex, $"While parsing '{fileName}'." ) )
                {
                    monitor.Info().Send( text );
                }
                return null;
            }
        }

        static bool ChechItemType( IActivityMonitor monitor, string fileName, IDependentItemContainer packageItem, IEnumerable<string> expectedItemTypes, SqlBaseItem result )
        {
            if( expectedItemTypes != null && !expectedItemTypes.Contains( result.ItemType ) )
            {
                monitor.Error().Send( $"Resource '{fileName}' of '{packageItem?.FullName}' is a '{result.ItemType}' whereas '{expectedItemTypes.Concatenate( "' or '" )}' is expected." );
                return false;
            }
            return true;
        }
    }
}
