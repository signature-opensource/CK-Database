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

    /// <summary>
    /// A user defined type is denoted by a dotted identifier [dbo].DefinedType or single identifier like geometry.
    /// </summary>
    public class SqlExprTypeDeclUserDefined : SqlExprBaseMultiIdentifier, ISqlExprUnifiedTypeDecl
    {
        public SqlExprTypeDeclUserDefined( IList<IAbstractExpr> tokens )
            : base( tokens, IsDotSeparator )
        {
        }

        internal SqlExprTypeDeclUserDefined( IAbstractExpr[] tokens )
            : base( tokens )
        {
        }

        public SqlDbType DbType { get { return SqlDbType.Udt; } }

        public new SqlExprTypeDeclUserDefined RemoveQuoteIfPossible( bool keepIfReservedKeyword )
        {
            IAbstractExpr[] c = base.RemoveQuoteIfPossible( keepIfReservedKeyword );
            return c != null ? new SqlExprTypeDeclUserDefined( c ) : this;
        }

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
