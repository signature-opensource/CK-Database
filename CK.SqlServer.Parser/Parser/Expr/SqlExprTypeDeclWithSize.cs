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
    public class SqlExprTypeDeclWithSize : SqlItem, ISqlExprUnifiedTypeDecl
    {
        readonly SqlToken[] _tokens;

        public SqlExprTypeDeclWithSize( SqlTokenIdentifier id )
        {
            if( id == null ) throw new ArgumentNullException( "id" );
            SqlDbType? dbType = SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType );
            if( !dbType.HasValue || (dbType != SqlDbType.Char && dbType != SqlDbType.VarChar && dbType != SqlDbType.NChar && dbType != SqlDbType.NVarChar && dbType != SqlDbType.Binary && dbType != SqlDbType.VarBinary) )
            {
                throw new ArgumentException( "Expected char, varchar, nchar, nvarchar, binary, varbinary.", "id" );
            }
            _tokens = CreateArray( id );
            DbType = dbType.Value;
            SyntaxSize = 0;
        }

        public SqlExprTypeDeclWithSize( SqlTokenIdentifier id, SqlTokenTerminal openPar, SqlToken size, SqlTokenTerminal closePar )
        {
            if( id == null ) throw new ArgumentNullException( "id" );
            SqlDbType? dbType = SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType );
            if( !dbType.HasValue || (dbType != SqlDbType.Char && dbType != SqlDbType.VarChar && dbType != SqlDbType.NChar && dbType != SqlDbType.NVarChar && dbType != SqlDbType.Binary && dbType != SqlDbType.VarBinary) )
            {
                throw new ArgumentException( "Expected char, varchar, nchar, nvarchar, binary, varbinary.", "id" );
            }
            if( openPar == null ) throw new ArgumentNullException( "openPar" );
            if( openPar.TokenType != SqlTokenType.OpenPar ) throw new ArgumentException( "Must be '('.", "openPar" );
            if( size == null ) throw new ArgumentNullException( "size" );
            if( !(size is SqlTokenLiteralInteger && ((SqlTokenLiteralInteger)size).Value > 0) 
                && !(size is SqlTokenIdentifier && ((SqlTokenIdentifier)size).TokenType == SqlTokenType.Max) ) throw new ArgumentException( "Size must be an integer greater than 0 or max.", "size" );
            if( closePar == null ) throw new ArgumentNullException( "closePar" );
            if( closePar.TokenType != SqlTokenType.ClosePar ) throw new ArgumentException( "Must be ')'.", "closePar" );
            _tokens = CreateArray( id, openPar, size, closePar );
            DbType = dbType.Value;
            SyntaxSize = size is SqlTokenLiteralInteger ? ((SqlTokenLiteralInteger)size).Value : -1;
        }

        internal SqlExprTypeDeclWithSize( SqlTokenIdentifier id, SqlDbType dbType )
        {
            Debug.Assert( id != null );
            Debug.Assert( dbType == SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType ).Value );
            Debug.Assert( dbType == SqlDbType.Char || dbType != SqlDbType.VarChar || dbType != SqlDbType.NChar || dbType != SqlDbType.NVarChar || dbType != SqlDbType.Binary || dbType != SqlDbType.VarBinary );
            _tokens = CreateArray( id );
            DbType = dbType;
            SyntaxSize = 0;
        }

        internal SqlExprTypeDeclWithSize( SqlTokenIdentifier id, SqlTokenTerminal openPar, SqlToken size, SqlTokenTerminal closePar, SqlDbType dbType )
        {
            Debug.Assert( id != null && openPar != null && size != null && closePar != null );
            Debug.Assert( openPar.TokenType == SqlTokenType.OpenPar && closePar.TokenType == SqlTokenType.ClosePar );
            Debug.Assert( dbType == SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType ).Value );
            Debug.Assert( dbType == SqlDbType.Char || dbType != SqlDbType.VarChar || dbType != SqlDbType.NChar || dbType != SqlDbType.NVarChar || dbType != SqlDbType.Binary || dbType != SqlDbType.VarBinary );
            Debug.Assert( (size is SqlTokenLiteralInteger && ((SqlTokenLiteralInteger)size).Value > 0) || (size is SqlTokenIdentifier && ((SqlTokenIdentifier)size).TokenType == SqlTokenType.Max) );
            _tokens = CreateArray( id, openPar, size, closePar );
            DbType = dbType;
            SyntaxSize = size is SqlTokenLiteralInteger ? ((SqlTokenLiteralInteger)size).Value : -1;
        }

        public override IEnumerable<ISqlItem> Items { get { return _tokens; } }

        public override IEnumerable<SqlToken> Tokens { get { return _tokens; } }

        public SqlTokenIdentifier TypeIdentifier { get { return (SqlTokenIdentifier)_tokens[0]; } }

        public SqlDbType DbType { get; private set; }

        public int SyntaxSize { get; private set; }

        public override SqlToken FirstOrEmptyToken { get { return _tokens[0]; } }

        public override SqlToken LastOrEmptyToken { get { return _tokens[_tokens.Length - 1]; } }
        
        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        byte ISqlExprUnifiedTypeDecl.SyntaxPrecision
        {
            get { return 0; }
        }

        byte ISqlExprUnifiedTypeDecl.SyntaxScale
        {
            get { return 0; }
        }

        int ISqlExprUnifiedTypeDecl.SyntaxSecondScale
        {
            get { return -1; }
        }

    }

}
