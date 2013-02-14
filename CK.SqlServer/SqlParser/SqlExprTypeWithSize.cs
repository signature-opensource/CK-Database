using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprTypeWithSize : SqlExpr, ISqlExprUnifiedType
    {
        readonly SqlToken[] _tokens;

        public SqlExprTypeWithSize( IEnumerable<SqlToken> tokens, SqlDbType dbType, int size = -2 )
        {
            if( tokens == null ) throw new ArgumentNullException( "tokens" );
            _tokens = tokens.ToArray();
            if( _tokens.Length != 1 && _tokens.Length != 4 ) throw new ArgumentException( "Expected char|varchar|nchar|nvarchar|binary|varbinary|smalldatetime|time[(f)]: 1 or 4 tokens." );
            if( !(_tokens[0] is SqlTokenIdentifier)
                || dbType != SqlReservedKeyword.FromSqlTokenTypeToSqlDbType( TypeIdentifier.TokenType ) )
            {
                throw new ArgumentException( "Invalid type.", "tokens" );
            }
            DbType = dbType;
            SyntaxSize = size;
        }

        public override IEnumerable<SqlToken> Tokens { get { return _tokens; } }

        public SqlTokenIdentifier TypeIdentifier 
        {
            get { return (SqlTokenIdentifier)_tokens[0]; } 
        }

        public SqlDbType DbType { get; private set; }

        public int SyntaxSize { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        byte ISqlExprUnifiedType.SyntaxPrecision
        {
            get { return 0; }
        }

        byte ISqlExprUnifiedType.SyntaxScale
        {
            get { return 0; }
        }

        int ISqlExprUnifiedType.SyntaxSecondScale
        {
            get { return -1; }
        }

    }

}
