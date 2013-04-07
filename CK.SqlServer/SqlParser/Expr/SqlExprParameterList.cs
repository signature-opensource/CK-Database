using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprParameterList : SqlExprBaseExprList<SqlExprParameter>
    {
        /// <summary>
        /// Initializes a new list of parameters with enclosing parenthesis.
        /// </summary>
        /// <param name="openPar">Opening parenthesis. Can not be null.</param>
        /// <param name="content">Comma separated list of <see cref="SqlExprParameter"/> (possibly empty).</param>
        /// <param name="closePar">Closing parenthesis. Can not be null.</param>
        public SqlExprParameterList( SqlTokenOpenPar openPar, IList<IAbstractExpr> content, SqlTokenClosePar closePar )
            : base( openPar, content, closePar, true )
        {
        }

        /// <summary>
        /// Initializes a new list of parameters without parenthesis.
        /// </summary>
        /// <param name="content">Comma separated list of <see cref="SqlExprParameter"/> (possibly empty).</param>
        public SqlExprParameterList( IList<IAbstractExpr> content )
            : base( content, true )
        {
        }

        internal SqlExprParameterList( IAbstractExpr[] newComponents )
            : base( newComponents )
        {
        }

        public override bool CanEnclose
        {
            get { return Opener.Count == 0; }
        }

        public override ISqlExprEnclosable Enclose( SqlTokenOpenPar opener, SqlTokenClosePar closer )
        {
            if( !CanEnclose ) throw new InvalidOperationException();
            return new SqlExprParameterList( EncloseComponents( opener, closer ) );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
