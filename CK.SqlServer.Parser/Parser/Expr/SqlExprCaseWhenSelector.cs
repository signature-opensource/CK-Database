using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{

    /// <summary>
    /// Defines "when Expression then ExpressionValue" items of <see cref="SqlExprCase"/> expression.
    /// </summary>
    public class SqlExprCaseWhenSelector : SqlNoExpr
    {
        public SqlExprCaseWhenSelector( IList<ISqlItem> items )
            : this( Build( items ) )
        {
        }

        static ISqlItem[] Build( IList<ISqlItem> items )
        {
            if( items == null )
            {
                throw new ArgumentNullException( "items" );
            }
            if( items.Count == 0 || items.Count % 4 != 0 ) throw new ArgumentException( "items must be not empty and its length must be a multiple of 4." );
            for( int i = 0; i < items.Count; ++i )
            {
                ISqlItem item = items[i];
                if( item == null ) throw new ArgumentException( String.Format( "Null item at {0}.", i ) );
                if( i % 4 == 0 )
                {
                    if( !SqlToken.IsUnquotedIdentifier( item, "when" ) ) throw new ArgumentException( String.Format( "Expected 'when' token at {0} but got {1}.", i, item ) );
                }
                else if( i % 4 == 1 )
                {
                    if( !(item is SqlExpr) ) throw new ArgumentException( String.Format( "Expected Expression at {0} but got {1}.", i, item ) );
                }
                else if( i % 4 == 2 )
                {
                    if( !SqlToken.IsUnquotedIdentifier( item, "then" ) ) throw new ArgumentException( String.Format( "Expected 'then' token at {0} but got {1}.", i, item ) );
                }
                else 
                {
                    if( !(item is SqlExpr) ) throw new ArgumentException( String.Format( "Expected Expression at {0} but got {1}.", i, item ) );
                }
            }
            return items.ToArray();
        }

        internal SqlExprCaseWhenSelector( ISqlItem[] newComponents )
            : base( newComponents )
        {
            Debug.Assert( Build( newComponents ) != null );
        }

        /// <summary>
        /// Gets the number of 'when E then V' selectors.
        /// </summary>
        public int Count { get { return Slots.Length / 4; } }

        public SqlTokenIdentifier WhenTokenAt( int i )
        {
            i *= 4;
            return (SqlTokenIdentifier)Slots[i];
        }

        public SqlExpr ExpressionAt( int i )
        {
            i *= 4;
            return (SqlExpr)Slots[i + 1];
        }

        public SqlTokenIdentifier ThenTokenAt( int i )
        {
            i *= 4;
            return (SqlTokenIdentifier)Slots[i + 2];
        }

        public SqlExpr ExpressionValueAt( int i )
        {
            i *= 4;
            return (SqlExpr)Slots[i + 3];
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
