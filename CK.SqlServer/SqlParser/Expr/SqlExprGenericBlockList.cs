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
    /// Comma separated list of <see cref="SqlExprGenericBlock"/> (possibly empty).
    /// </summary>
    public class SqlExprGenericBlockList : SqlExprBaseExprList<SqlExprGenericBlock>
    {
        /// <summary>
        /// Initializes a new list of blocks enclosed in parenthesis.
        /// </summary>
        /// <param name="openPar">Opening parenthesis. Can not be null.</param>
        /// <param name="tokens">Comma separated list of <see cref="SqlExprGenericBlock"/> (possibly empty).</param>
        /// <param name="closePar">Closing parenthesis. Can not be null.</param>
        public SqlExprGenericBlockList( SqlTokenOpenPar openPar, IList<IAbstractExpr> tokens, SqlTokenClosePar closePar )
            : base( openPar, tokens, closePar, true )
        {
        }

        /// <summary>
        /// Initializes a new list of blocks without parenthesis.
        /// </summary>
        /// <param name="tokens">Comma separated list of <see cref="SqlExprGenericBlock"/> (possibly empty).</param>
        public SqlExprGenericBlockList( IList<IAbstractExpr> tokens )
            : base( tokens, true )
        {
        }

        internal SqlExprGenericBlockList( IAbstractExpr[] newComponents )
            : base( newComponents )
        {
        }

        public override bool CanEnclose
        {
            get { return true; }
        }

        public override ISqlExprEnclosable Enclose( SqlExprMultiToken<SqlTokenOpenPar> opener, SqlExprMultiToken<SqlTokenClosePar> closer )
        {
            return new SqlExprList( EncloseComponents( opener, closer ) );
        }

        internal ISqlExprList<SqlExpr> LiftedContent
        {
            get
            {
                IAbstractExpr[] lifted = ReplaceNonSeparator( b => b.LiftedExpression );
                if( lifted == null ) return this;
                return new SqlExprList( lifted );
            }
        }


        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
