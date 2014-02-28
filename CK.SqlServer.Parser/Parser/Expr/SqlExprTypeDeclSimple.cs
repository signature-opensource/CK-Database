using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    public class SqlExprTypeDeclSimple : SqlItem, ISqlExprUnifiedTypeDecl
    {
        readonly SqlTokenIdentifier[] _tokens;

        public SqlExprTypeDeclSimple( SqlTokenIdentifier id )
        {
            SqlDbType? dbType = SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType );
            if( !dbType.HasValue )
            {
                throw new ArgumentException( "Invalid type.", "id" );
            }
            DbType = dbType.Value;
            _tokens = CreateArray( id );
        }

        internal SqlExprTypeDeclSimple( SqlTokenIdentifier id, SqlDbType dbType )
        {
            Debug.Assert( dbType == SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType ) );
            DbType = dbType;
            _tokens = CreateArray( id );
        }

        public SqlDbType DbType { get; private set; }

        public override IEnumerable<ISqlItem> Components { get { return _tokens; } }

        public override IEnumerable<SqlToken> Tokens { get { return _tokens; } }

        public SqlTokenIdentifier TypeIdentifier { get { return (SqlTokenIdentifier)_tokens[0]; } }

        public override SqlToken FirstOrEmptyToken { get { return _tokens[0]; } }

        public override SqlToken LastOrEmptyToken { get { return _tokens[_tokens.Length - 1]; } }

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

        int ISqlExprUnifiedTypeDecl.SyntaxSecondScale
        {
            get { return -1; }
        }
    }

}
