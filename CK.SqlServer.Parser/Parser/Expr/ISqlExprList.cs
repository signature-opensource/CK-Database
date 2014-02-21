using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public interface ISqlExprList<out T> : IReadOnlyList<T> where T : SqlItem
    {
        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        int SeparatorCount { get; }

        /// <summary>
        /// Gets the separators.
        /// </summary>
        IEnumerable<ISqlItem> SeparatorTokens { get; }
        
    }
}
