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
    public class SqlExprTypeDateAndTime : SqlExpr, ISqlExprUnifiedType
    {
        SqlToken[] _tokens;

        public SqlExprTypeDateAndTime( IEnumerable<SqlToken> tokens, SqlDbType dbType, int secondScale = -1 )
        {
            if( tokens == null ) throw new ArgumentNullException( "tokens" );
            _tokens = tokens.ToArray();
            if( _tokens.Length != 1 && _tokens.Length != 4 ) throw new ArgumentException( "Expected date|datetime|datetime2|datetimeoffset|smalldatetime|time[(f)]: 1 or 4 tokens." );
            if( !(_tokens[0] is SqlTokenIdentifier)
                || dbType != SqlReservedKeyword.FromSqlTokenTypeToSqlDbType( TypeIdentifier.TokenType ) )
            {
                throw new ArgumentException( "Invalid date/time type.", "tokens" );
            }
            DbType = dbType;
            SyntaxSecondScale = secondScale;
        }
        
        public override IEnumerable<SqlToken> Tokens { get { return _tokens; } }

        public SqlTokenIdentifier TypeIdentifier 
        {
            get { return (SqlTokenIdentifier)_tokens[0]; } 
        }

        public SqlDbType DbType { get; private set; }

        public int SyntaxSecondScale { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        int ISqlExprUnifiedType.SyntaxSize
        {
            get { return -2; }
        }

        byte ISqlExprUnifiedType.SyntaxPrecision
        {
            get { return 0; }
        }

        byte ISqlExprUnifiedType.SyntaxScale
        {
            get { return 0; }
        }

    }

}
