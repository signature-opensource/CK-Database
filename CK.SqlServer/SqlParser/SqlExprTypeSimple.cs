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
    public class SqlExprTypeSimple : SqlExprBaseMonoToken, ISqlExprUnifiedType
    {
        public SqlExprTypeSimple( SqlTokenIdentifier token, SqlDbType dbType )
            : base( token )
        {
            if( dbType != SqlReservedKeyword.FromSqlTokenTypeToSqlDbType( Token.TokenType ) )
            {
                throw new ArgumentException( "Invalid type.", "token" );
            }
            DbType = dbType;
        }

        public SqlDbType DbType { get; private set; }

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

        int ISqlExprUnifiedType.SyntaxSecondScale
        {
            get { return -1; }
        }
    }

}
