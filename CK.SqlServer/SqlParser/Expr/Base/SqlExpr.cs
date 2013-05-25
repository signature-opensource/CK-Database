using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using CK.Core;
using System.Diagnostics;
using System.Globalization;

namespace CK.SqlServer
{
    /// <summary>
    /// SqlExpr is a SqlItem with optionals <see cref="Opener"/> and <see cref="Closer"/>.
    /// </summary>
    public abstract class SqlExpr : SqlItem
    {
        readonly protected ISqlItem[] Slots;

        internal SqlExpr( ISqlItem[] slots )
        {
            Debug.Assert( slots != null && slots.Length >= 2 && slots[0] is SqlExprMultiToken<SqlTokenOpenPar> && slots[slots.Length - 1] is SqlExprMultiToken<SqlTokenClosePar> );
            Slots = slots;
        }

        /// <summary>
        /// Gets the opening parenthesis. Can be empty.
        /// </summary>
        public SqlExprMultiToken<SqlTokenOpenPar> Opener { get { return (SqlExprMultiToken<SqlTokenOpenPar>)Slots[0]; } }

        /// <summary>
        /// Gets the closing parenthesis. Can be empty.
        /// </summary>
        public SqlExprMultiToken<SqlTokenClosePar> Closer { get { return (SqlExprMultiToken<SqlTokenClosePar>)Slots[Slots.Length-1]; } }

        /// <summary>
        /// Gets the last token of the expression.
        /// </summary>
        public sealed override SqlToken LastOrEmptyToken { get { return Closer.Count > 0 ? Closer.LastOrEmptyToken : Slots[Slots.Length - 2].LastOrEmptyToken; } }

        /// <summary>
        /// Gets the first token of the expression.
        /// </summary>
        public sealed override SqlToken FirstOrEmptyToken { get { return Opener.Count > 0 ? Opener.FirstOrEmptyToken : Slots[1].FirstOrEmptyToken; } }

        /// <summary>
        /// Gets the items of this expression: it is a mix of <see cref="SqlToken"/> and <see cref="SqlItem"/>.
        /// Never null nor empty since an expression has at least an opener and a closer (even if they are empty).
        /// </summary>
        public sealed override IEnumerable<ISqlItem> Components { get { return Slots; } }

        /// <summary>
        /// Gets the sql items without the enclosing parenthesis if they exist.
        /// </summary>
        public IEnumerable<ISqlItem> ItemsWithoutParenthesis { get { return Slots.Skip( 1 ).Take( Slots.Length - 2 ); } }

        /// <summary>
        /// Gets the tokens without the enclosing parenthesis if they exist.
        /// </summary>
        public IEnumerable<SqlToken> TokensWithoutParenthesis { get { return Flatten( ItemsWithoutParenthesis ); } }

        internal SqlExpr MutableEnclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            Slots[0] = SqlExprMultiToken<SqlTokenOpenPar>.Create( openPar, Opener );
            Slots[Slots.Length-1] = SqlExprMultiToken<SqlTokenClosePar>.Create( Closer, closePar );
            return this;
        }

        protected ISqlItem[] EncloseComponents( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            return CreateEnclosedArray( openPar, Slots, closePar );
        }

    }

}
