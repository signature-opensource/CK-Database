using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public class SqlExprTypeDeclDateAndTime : SqlItem, ISqlExprUnifiedTypeDecl
    {
        readonly SqlToken[] _tokens;

        public SqlExprTypeDeclDateAndTime( SqlTokenIdentifier id )
        {
            if( id == null ) throw new ArgumentNullException( "token" );
            _tokens = CreateArray( id );
            SqlDbType? dbType = SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType );
            if( !dbType.HasValue
                || (dbType.Value != SqlDbType.DateTime2 && dbType.Value != SqlDbType.Time && dbType.Value != SqlDbType.DateTimeOffset && dbType.Value != SqlDbType.DateTime && dbType.Value != SqlDbType.Date && dbType.Value != SqlDbType.SmallDateTime) )
            {
                throw new ArgumentException( "Invalid date/time type (must be date, datetime, smalldatetime, datetime2, time or datetimeoffset).", "id" );
            }
            DbType = dbType.Value;
            SyntaxSecondScale = -1;
        }

        public SqlExprTypeDeclDateAndTime( SqlTokenIdentifier id, SqlTokenTerminal openPar, SqlTokenLiteralInteger secondScale, SqlTokenTerminal closePar )
        {
            if( id == null ) throw new ArgumentNullException( "id" );
            SqlDbType? dbType = SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType );
            if( !dbType.HasValue || (dbType.Value != SqlDbType.DateTime2 && dbType.Value != SqlDbType.Time && dbType.Value != SqlDbType.DateTimeOffset) )
            {
                throw new ArgumentException( "Invalid date/time type (must be datetime2, time or datetimeoffset).", "id" );
            }
            if( openPar == null ) throw new ArgumentNullException( "openPar" );
            if( openPar.TokenType != SqlTokenType.OpenPar ) throw new ArgumentException( "Must be '('.", "openPar" );
            if( secondScale == null ) throw new ArgumentNullException( "secondScale" );
            if( secondScale.Value > 7 ) throw new ArgumentException( "Fractional seconds precision must be less or equal to 7.", "secondScale" );
            if( closePar == null ) throw new ArgumentNullException( "closePar" );
            if( closePar.TokenType != SqlTokenType.ClosePar ) throw new ArgumentException( "Must be ')'.", "closePar" );
            _tokens = CreateArray( id, openPar, secondScale, closePar );
            DbType = dbType.Value;
            SyntaxSecondScale = secondScale.Value;
        }

        internal SqlExprTypeDeclDateAndTime( SqlTokenIdentifier id, SqlTokenTerminal openPar, SqlTokenLiteralInteger secondScale, SqlTokenTerminal closePar, SqlDbType dbType )
        {
            Debug.Assert( id != null && openPar != null && secondScale != null && closePar != null );
            Debug.Assert( openPar.TokenType == SqlTokenType.OpenPar && closePar.TokenType == SqlTokenType.ClosePar );
            Debug.Assert( dbType == SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType ).Value && (dbType == SqlDbType.DateTime2 || dbType == SqlDbType.Time || dbType == SqlDbType.DateTimeOffset) );
            Debug.Assert( secondScale.Value >= 0 && secondScale.Value <= 7 );

            _tokens = CreateArray( id, openPar, secondScale, closePar );
            DbType = dbType;
            SyntaxSecondScale = secondScale.Value;
        }

        internal SqlExprTypeDeclDateAndTime( SqlTokenIdentifier id, SqlDbType dbType )
        {
            Debug.Assert( id != null );
            Debug.Assert( SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType ).Value == dbType );
            _tokens = CreateArray( id );
            DbType = dbType;
            SyntaxSecondScale = -1;
        }

        public override IEnumerable<ISqlItem> Items { get { return _tokens; } }

        public override IEnumerable<SqlToken> Tokens { get { return _tokens; } }

        public override SqlToken FirstOrEmptyT { get { return _tokens[0]; } }

        public override SqlToken LastOrEmptyT { get { return _tokens[_tokens.Length - 1]; } }

        public SqlTokenIdentifier TypeIdentifierT { get { return (SqlTokenIdentifier)_tokens[0]; } }

        public SqlDbType DbType { get; private set; }

        public int SyntaxSecondScale { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        int ISqlExprUnifiedTypeDecl.SyntaxSize
        {
            get { return -2; }
        }

        byte ISqlExprUnifiedTypeDecl.SyntaxPrecision
        {
            get { return 0; }
        }

        byte ISqlExprUnifiedTypeDecl.SyntaxScale
        {
            get { return 0; }
        }

    }

}
