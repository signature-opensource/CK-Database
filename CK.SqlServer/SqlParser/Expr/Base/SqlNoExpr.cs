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
    /// Non enclosable items specializes this base class. SqlNoExpr is the base class for <see cref="SqlExprBaseSt">statements</see>,
    /// but can also be used for parts of a <see cref="SqlExpr"/>.
    /// </summary>
    public abstract class SqlNoExpr : SqlItem
    {
        readonly protected ISqlItem[] Slots;

        internal SqlNoExpr( ISqlItem[] slots )
        {
            Debug.Assert( slots != null );
            Slots = slots;
        }

        /// <summary>
        /// Gets the components of this expression: it is a mix of <see cref="SqlToken"/> and <see cref="SqlExpr"/>.
        /// Never null but can be empty.
        /// </summary>
        public override IEnumerable<ISqlItem> Components { get { return Slots; } }

        /// <summary>
        /// Gets the first token of the expression.
        /// </summary>
        public sealed override SqlToken FirstOrEmptyToken { get { return Slots.Length > 0 ? Slots[0].FirstOrEmptyToken : SqlToken.Empty; } }

        /// <summary>
        /// Gets the last token of the expression.
        /// </summary>
        public sealed override SqlToken LastOrEmptyToken { get { return Slots.Length > 0 ? Slots[Slots.Length - 1].LastOrEmptyToken : SqlToken.Empty; } }

    }

}
