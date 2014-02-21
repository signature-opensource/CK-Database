using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{

    public interface ISqlIdentifier : ISqlItem
    {
        /// <summary>
        /// Gets the number of <see cref="Identifiers"/>.
        /// </summary>
        int IdentifiersCount { get; }

        /// <summary>
        /// Gets the <see cref="SqlTokenIdentifier"/> (without the separators).
        /// </summary>
        IEnumerable<SqlTokenIdentifier> Identifiers { get; }

        /// <summary>
        /// Gets the <see cref="SqlTokenIdentifier"/> (without the separators).
        /// </summary>
        SqlTokenIdentifier IdentifierAt( int i );

        /// <summary>
        /// Gets whether this identifier is a variable.
        /// </summary>
        bool IsVariable { get; }

        /// <summary>
        /// Gets the sql items (<see cref="SqlTokenIdentifier"/> and <see cref="SqlTokenTerminal"/> for the separators) without 
        /// the enclosing parenthesis if they exist.
        /// </summary>
        IEnumerable<SqlToken> TokensWithoutParenthesis { get; }

    }
}
