using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Parser
{
    public static class SqlTokenTypeExtension
    {
        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a select operator: <see cref="SqlTokenType.Union"/>, <see cref="SqlTokenType.Except"/>, 
        /// <see cref="SqlTokenType.Intersect"/>, <see cref="SqlTokenType.Order"/> and <see cref="SqlTokenType.For"/>.
        /// </summary>
        /// <param name="this">Token type.</param>
        /// <returns>Whether the token is a select operator.</returns>
        static public bool IsSelectOperator( this SqlTokenType @this )
        {
            return @this == SqlTokenType.Union
                    || @this == SqlTokenType.Except
                    || @this == SqlTokenType.Intersect
                    || @this == SqlTokenType.Order
                    || @this == SqlTokenType.For;
        }

        /// <summary>
        /// True if the token is a @variable (or @@SystemFunction like @@RowCount) or a 
        /// literal value ('string' or 0x5454 number for instance).
        /// </summary>
        /// <param name="this">Token to test.</param>
        /// <returns>True for a variable or a literal.</returns>
        static public bool IsVariableNameOrLiteral( this SqlTokenType @this )
        {
            return @this == SqlTokenType.IdentifierVariable || (@this > 0 && (@this & SqlTokenType.LitteralMask) != 0);
        }

        /// <summary>
        /// True if the token is a @variable (or @@SystemFunction like @@RowCount) or a 
        /// literal value ('string' or 0x5454 number for instance) or null.
        /// </summary>
        /// <param name="this">Token to test.</param>
        /// <returns>True for a variable, a literal or null.</returns>
        static public bool IsVariableNameOrLiteralOrNull( SqlTokenType @this )
        {
            return @this == SqlTokenType.IdentifierVariable || @this == SqlTokenType.Null || (@this > 0 && (@this & SqlTokenType.LitteralMask) != 0);
        }

        /// <summary>
        /// True if this <see cref="SqlTokenType"/> denotes a reserved keyword that starts a statement (select, create, declare, etc.)
        /// or a standard identifer that also can start a statement (throw, get, move, etc.).
        /// </summary>
        /// <param name="this">Token to test.</param>
        static public bool IsStartStatement( this SqlTokenType type )
        {
            Debug.Assert( SqlTokenType.IdentifierStandardStatement == SqlTokenType.IsIdentifier
                            && SqlTokenType.IdentifierReservedStatement == (SqlTokenType.IsIdentifier + (1 << 11)), "Statement identifiers must be the first ones." );
            return (type & SqlTokenType.IdentifierTypeMask) <= SqlTokenType.IdentifierReservedStatement;
        }

        /// <summary>
        /// True if this <see cref="SqlTokenType"/> denotes a reserved keyword that starts a statement (select, create, declare, etc.)
        /// or a standard identifer that also can start a statement (throw, get, move, etc.) or WITH.
        /// </summary>
        /// <param name="this">Token to test.</param>
        static public bool IsStartStatementOrWith( this SqlTokenType type )
        {
            return IsStartStatement( type ) || type == SqlTokenType.With;
        }



    }
}
