using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// Composite base for token manipulation.
    /// Ultimate abstraction of an expression contains at least one <see cref="SqlToken"/>.
    /// </summary>
    public interface IAbstractExpr
    {
        /// <summary>
        /// Gets the tokens that compose this expression.
        /// Never null nor empty: an expression covers at least one token.
        /// </summary>
        IEnumerable<SqlToken> Tokens { get; }
    }
}
