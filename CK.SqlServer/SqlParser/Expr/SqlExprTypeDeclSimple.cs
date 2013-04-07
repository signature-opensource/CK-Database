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
    public class SqlExprTypeDeclSimple : SqlExprBaseMonoToken<SqlTokenIdentifier>, ISqlExprUnifiedTypeDecl
    {
        public SqlExprTypeDeclSimple( SqlTokenIdentifier id )
            : base( id )
        {
            SqlDbType? dbType = SqlReservedKeyword.FromSqlTokenTypeToSqlDbType( Token.TokenType );
            if( !dbType.HasValue )
            {
                throw new ArgumentException( "Invalid type.", "id" );
            }
            DbType = dbType.Value;
        }

        internal SqlExprTypeDeclSimple( SqlTokenIdentifier token, SqlDbType dbType )
            : base( token )
        {
            Debug.Assert( dbType == SqlReservedKeyword.FromSqlTokenTypeToSqlDbType( Token.TokenType ) );
            DbType = dbType;
        }

        public SqlDbType DbType { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
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
