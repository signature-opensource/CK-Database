using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// </summary>
    public class SqlExprStUnmodeled : SqlExprBaseSt
    {
        readonly SqlTokenIdentifier _id;
        readonly IReadOnlyList<IAbstractExpr> _expr;

        public SqlExprStUnmodeled( SqlTokenIdentifier id, IEnumerable<SqlExpr> expressions, SqlTokenTerminal statementTerminator = null )
            : base( statementTerminator )
        {
            _id = id;
            _expr = new ReadOnlyListMono<IAbstractExpr>( id ).Concat( expressions ).ToReadOnlyList();
        }

        protected override IEnumerable<SqlToken> GetStatementTokens()
        {
            return Flatten( _expr );
        }

        public SqlTokenIdentifier Identifier { get { return _id; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
