using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// Captures a select column definition. 
    /// </summary>
    public class SelectColumn : SqlNoExpr
    {
        readonly ISqlIdentifier _colName;
        readonly SqlToken _asOrEqual;
        readonly SqlExpr _definition;

        static readonly SqlTokenIdentifier _autoAsToken = new SqlTokenIdentifier( SqlTokenType.IdentifierStandard, "as", SqlTrivia.OneSpace, SqlTrivia.OneSpace );
        static readonly SqlTokenIdentifier _autoAsTokenNoLeft = new SqlTokenIdentifier( SqlTokenType.IdentifierStandard, "as", null, SqlTrivia.OneSpace );
        static readonly SqlTokenIdentifier _autoAsTokenNoRight = new SqlTokenIdentifier( SqlTokenType.IdentifierStandard, "as", SqlTrivia.OneSpace, null );
        static readonly SqlTokenIdentifier _autoAsTokenNoSpace = new SqlTokenIdentifier( SqlTokenType.IdentifierStandard, "as", null, null );

        public SelectColumn( ISqlIdentifier colName, SqlTokenTerminal assignToken, SqlExpr definition )
            : this( Build( colName, assignToken, definition ) )
        {
        }

        public SelectColumn( SqlExpr definition, SqlTokenIdentifier asToken, ISqlIdentifier colName )
            : this( Build( definition, asToken, colName ) )
        {
        }

        public SelectColumn( SqlExpr definition, ISqlIdentifier colName = null )
            : this( Build( definition, colName ) )
        {
        }

        static ISqlItem[] Build( ISqlIdentifier colName, SqlTokenTerminal assignToken, SqlExpr definition )
        {
            if( colName == null ) throw new ArgumentNullException( "colName" );
            if( assignToken == null ) throw new ArgumentNullException( "assignToken" );
            if( assignToken.TokenType != SqlTokenType.Assign ) throw new ArgumentException( "Assign token expected.", "assignToken" );
            if( definition == null ) throw new ArgumentNullException( "definition" );
            return CreateArray( colName, assignToken, definition );
        }

        static ISqlItem[] Build( SqlExpr definition, SqlTokenIdentifier asToken, ISqlIdentifier colName )
        {
            if( definition == null ) throw new ArgumentNullException( "definition" );
            if( colName == null ) throw new ArgumentNullException( "colName" );
            if( asToken == null )
            {
                var leftTrivia = definition.LastOrEmptyToken.TrailingTrivia;
                var rightTrivia = colName.FirstOrEmptyToken.LeadingTrivia;
                if( leftTrivia.Count == 0 )
                {
                    if( rightTrivia.Count == 0 ) asToken = _autoAsToken;
                    else asToken = _autoAsTokenNoRight;
                }
                else
                {
                    if( rightTrivia.Count == 0 )
                        asToken = _autoAsTokenNoLeft;
                    else asToken = _autoAsTokenNoSpace;
                }
            }
            else if( !asToken.NameEquals( "as" ) ) throw new ArgumentException( "As token expected.", "asToken" );
            return CreateArray( definition, asToken, colName );
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

        public SqlToken AsOrEqual { get { return _asOrEqual; } }
        
        public SqlExpr Definition { get { return _definition; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
