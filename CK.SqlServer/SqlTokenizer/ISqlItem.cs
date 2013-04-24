using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// Composite base for token or expression manipulation.
    /// </summary>
    public interface ISqlItem
    {
        /// <summary>
        /// Gets the tokens that compose this expression.
        /// Never null but can be empty for empty opener or closer <see cref="SqlToken.EmptyOpenPar"/> or <see cref="SqlToken.EmptyClosePar"/>.
        /// </summary>
        IEnumerable<SqlToken> Tokens { get; }

        /// <summary>
        /// Gets the last token of the expression or, in worst case when this is either <see cref="SqlToken.EmptyOpenPar"/> or <see cref="SqlToken.EmptyClosePar"/>, gets <see cref="SqlToken.Empty"/>.
        /// This is mainly to ease trivias manipulation around expressions.
        /// </summary>
        SqlToken LastOrEmptyToken { get; }

        /// <summary>
        /// Gets the first token of the expression or, in worst case when this is either <see cref="SqlToken.EmptyOpenPar"/> or <see cref="SqlToken.EmptyClosePar"/>, gets <see cref="SqlToken.Empty"/>.
        /// This is mainly to ease trivias manipulation around expressions.
        /// </summary>
        SqlToken FirstOrEmptyToken { get; }

    }
}
