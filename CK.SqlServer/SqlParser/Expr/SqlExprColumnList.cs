using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprColumnList : SqlExprBaseExprList<SqlExprIdentifier>
    {
        /// <summary>
        /// Initializes a new list of columns with optional enclosing parentheses.
        /// </summary>
        /// <param name="openPar">Opening parenthesis. Can not be null.</param>
        /// <param name="tokens">Comma separated list of <see cref="SqlExprIdentifier"/> (can not be empty).</param>
        /// <param name="closePar">Closing parenthesis. Can not be null.</param>
        public SqlExprColumnList( SqlTokenOpenPar openPar, IList<IAbstractExpr> tokens, SqlTokenClosePar closePar )
            : base( openPar, tokens, closePar, false )
        {
        }

        internal SqlExprColumnList( IAbstractExpr[] newComponents )
            : base( newComponents )
        {
            Debug.Assert( NonSeparatorCount > 0, "Column list must not be empty." );
        }

        public override bool CanEnclose
        {
            get { return Opener.Count == 0; }
        }

        public override ISqlExprEnclosable Enclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            if( !CanEnclose ) throw new InvalidOperationException();
            return new SqlExprColumnList( EncloseComponents( openPar, closePar ) );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
