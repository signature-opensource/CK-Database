using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Captures a select column definition. 
    /// </summary>
    public class SelectColumn : SqlNoExpr
    {
        readonly ISqlIdentifier _colName;
        readonly SqlToken _asOrEqual;
        readonly SqlExpr _definition;

        static readonly SqlTokenIdentifier _autoAsTok = new SqlTokenIdentifier( SqlTokenType.As, "as", SqlTrivia.OneSpace, SqlTrivia.OneSpace );
        static readonly SqlTokenIdentifier _autoAsTokNoLeft = new SqlTokenIdentifier( SqlTokenType.As, "as", null, SqlTrivia.OneSpace );
        static readonly SqlTokenIdentifier _autoAsTokNoRight = new SqlTokenIdentifier( SqlTokenType.As, "as", SqlTrivia.OneSpace, null );
        static readonly SqlTokenIdentifier _autoAsTokNoSpace = new SqlTokenIdentifier( SqlTokenType.As, "as", null, null );

        public SelectColumn( ISqlIdentifier colName, SqlTokenTerminal assignTok, SqlExpr definition )
            : this( Build( colName, assignTok, definition ) )
        {
        }

        public SelectColumn( SqlExpr definition, SqlTokenIdentifier asTok, ISqlIdentifier colName )
            : this( Build( definition, asTok, colName ) )
        {
        }

        public SelectColumn( SqlExpr definition, ISqlIdentifier colName = null )
            : this( Build( definition, colName ) )
        {
        }

        static ISqlItem[] Build( ISqlIdentifier colName, SqlTokenTerminal assignTok, SqlExpr definition )
        {
            if( colName == null ) throw new ArgumentNullException( "colName" );
            if( assignTok == null ) throw new ArgumentNullException( "assignTok" );
            if( assignTok.TokenType != SqlTokenType.Assign ) throw new ArgumentException( "Assign token expected.", "assignTok" );
            if( definition == null ) throw new ArgumentNullException( "definition" );
            return CreateArray( colName, assignTok, definition );
        }

        static ISqlItem[] Build( SqlExpr definition, SqlTokenIdentifier asTok, ISqlIdentifier colName )
        {
            if( definition == null ) throw new ArgumentNullException( "definition" );
            if( colName == null ) throw new ArgumentNullException( "colName" );
            if( asTok == null )
            {
                var leftTrivia = definition.LastOrEmptyToken.TrailingTrivia;
                var rightTrivia = colName.FirstOrEmptyToken.LeadingTrivia;
                if( leftTrivia.Count == 0 )
                {
                    if( rightTrivia.Count == 0 ) asTok = _autoAsTok;
                    else asTok = _autoAsTokNoRight;
                }
                else
                {
                    if( rightTrivia.Count == 0 )
                        asTok = _autoAsTokNoLeft;
                    else asTok = _autoAsTokNoSpace;
                }
            }
            else if( asTok.TokenType != SqlTokenType.As ) throw new ArgumentException( "As token expected.", "asTok" );
            return CreateArray( definition, asTok, colName );
        }

        static ISqlItem[] Build( SqlExpr definition, ISqlIdentifier colName )
        {
            if( definition == null ) throw new ArgumentNullException( "definition" );
            if( colName == null ) return CreateArray( definition );
            return Build( definition, null, colName );
        }

        internal SelectColumn( ISqlItem[] items )
            : base( items )
        {
            if( Slots.Length == 1 ) _definition = (SqlExpr)Slots[0];
            else
            {
                _asOrEqual = (SqlToken)Slots[1];
                if( _asOrEqual is SqlTokenTerminal )
                {
                    _colName = (ISqlIdentifier)Slots[0];
                    _definition = (SqlExpr)Slots[2];
                }
                else
                {
                    _colName = (ISqlIdentifier)Slots[2];
                    _definition = (SqlExpr)Slots[0];
                }
            }
        }

        public ISqlIdentifier ColumnName { get { return _colName; } }

        public bool IsEqualSyntax { get { return _asOrEqual is SqlTokenTerminal; } }

        public bool IsAsSyntax { get { return _asOrEqual is SqlTokenIdentifier; } }

        public SqlToken AsOrEqualTok { get { return _asOrEqual; } }
        
        public SqlExpr Definition { get { return _definition; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
