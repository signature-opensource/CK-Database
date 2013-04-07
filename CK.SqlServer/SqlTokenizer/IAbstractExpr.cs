using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// Composite base for token or expression manipulation.
    /// </summary>
    public interface IAbstractExpr
    {
        /// <summary>
        /// Gets the tokens that compose this expression.
        /// Never null but can be empty.
        /// </summary>
        IEnumerable<SqlToken> Tokens { get; }
    }
}
