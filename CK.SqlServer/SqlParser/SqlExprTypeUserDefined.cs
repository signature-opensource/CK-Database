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
    public class SqlExprTypeUserDefined : SqlExprBaseListWithSeparator<SqlTokenIdentifier>, ISqlExprUnifiedType
    {

        public SqlExprTypeUserDefined( IEnumerable<SqlToken> tokens )
            : base( tokens )
        {
        }

        public int IdentifierCount { get { return NonSeparatorCount; } }

        public IEnumerable<SqlTokenIdentifier> Identifiers { get { return NonSeparatorTokens; } }

        public SqlDbType DbType { get { return SqlDbType.Udt; } }

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
