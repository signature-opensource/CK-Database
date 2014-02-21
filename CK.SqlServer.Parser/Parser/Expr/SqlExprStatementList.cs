using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// List of <see cref="SqlExprBaseSt">statements</see>. 
    /// It is not a statement itself: the <see cref="SqlExprStBlock"/> is the composite statement (begin...end).
    /// </summary>
    public class SqlExprStatementList : SqlItem, IReadOnlyList<SqlExprBaseSt>
    {
        readonly SqlExprBaseSt[] _statements;

        public SqlExprStatementList( IEnumerable<SqlExprBaseSt> statements )
        {
            _statements = statements.ToArray();
        }

        internal SqlExprStatementList( SqlExprBaseSt[] newStatements )
        {
            _statements = newStatements;
        }

        /// <summary>
        /// Gets the list of statements.
        /// </summary>
        public IReadOnlyList<SqlExprBaseSt> Statements
        {
            get { return this; }
        }

        public override sealed IEnumerable<ISqlItem> Components
        {
            get { return _statements; }
        }

        public override SqlToken FirstOrEmptyToken { get { return _statements[0].FirstOrEmptyToken; } }

        public override SqlToken LastOrEmptyToken { get { return _statements[_statements.Length-1].LastOrEmptyToken; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        #region IReadOnlyList<SqlExprBaseSt> Members

        SqlExprBaseSt IReadOnlyList<SqlExprBaseSt>.this[int index]
        {
            get { return _statements[index]; }
        }


        int IReadOnlyCollection<SqlExprBaseSt>.Count
        {
            get { return _statements.Length; }
        }

        IEnumerator<SqlExprBaseSt> IEnumerable<SqlExprBaseSt>.GetEnumerator()
        {
            return (IEnumerator<SqlExprBaseSt>)_statements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (IEnumerator<SqlExprBaseSt>)_statements.GetEnumerator();
        }

        #endregion
    }


}
