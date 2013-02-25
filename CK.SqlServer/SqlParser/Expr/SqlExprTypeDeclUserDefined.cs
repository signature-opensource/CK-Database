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
    /// A user defined type is denoted by a dotted identifier [dbo].DefinedType or sinlge identifier like geometry.
    /// </summary>
    public class SqlExprTypeDeclUserDefined : SqlExprBaseListWithSeparator<SqlTokenIdentifier>, ISqlExprUnifiedTypeDecl
    {
        public SqlExprTypeDeclUserDefined( IEnumerable<SqlToken> tokens )
            : base( tokens, false, IsDotSeparator )
        {
        }

        internal SqlExprTypeDeclUserDefined( IAbstractExpr[] tokens )
            : base( tokens )
        {
            Debug.Assert( tokens != null );
            DebugCheckArray( tokens, false, IsDotSeparator );
        }

        public int IdentifierCount { get { return NonSeparatorCount; } }

        public IEnumerable<SqlTokenIdentifier> Identifiers { get { return NonSeparatorTokens; } }

        public SqlDbType DbType { get { return SqlDbType.Udt; } }

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
