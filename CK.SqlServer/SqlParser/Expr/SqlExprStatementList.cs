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
    /// List of <see cref="SqlExprBaseSt">statements</see>. 
    /// It is not a statement itself: the <see cref="SqlExprStBlock"/> is the composite statement (begin...end).
    /// </summary>
    public class SqlExprStatementList : SqlExpr
    {
        readonly IReadOnlyList<SqlExprBaseSt> _statements;

        public SqlExprStatementList( IEnumerable<SqlExprBaseSt> statements )
        {
            _statements = statements.ToReadOnlyList();
        }

        /// <summary>
        /// Gets the list of statements.
        /// </summary>
        public IReadOnlyList<SqlExpr> Statements
        {
            get { return _statements; }
        }

        public override IEnumerable<SqlToken> Tokens
        {
            get { return Flatten( _statements ); }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
