using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.SqlServer
{
    public interface ISqlExprList<out T> : ISqlExprEnclosable, IReadOnlyList<T> where T : SqlExpr
    {
        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        int SeparatorCount { get; }

        /// <summary>
        /// Gets the separators.
        /// </summary>
        IEnumerable<IAbstractExpr> SeparatorTokens { get; }
        
    }
}
