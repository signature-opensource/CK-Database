#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Tokenizer\SqlTokenExtension.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    public static class SqlExprExtension
    {
        /// <summary>
        /// Writes an <see cref="IEnumerable"/> of <see cref="SqlToken"/> without its trivias. 
        /// Calls <see cref="SqlToken.WriteWithoutTrivias"/> on each token.
        /// </summary>
        /// <param name="this">An <see cref="IEnumerable"/> of <see cref="SqlToken"/></param>
        /// <param name="separator">Separator between tokens.</param>
        /// <param name="b">StringBuilder to write into.</param>
        public static void WriteTokensWithoutTrivias( this IEnumerable<SqlToken> @this, string separator, StringBuilder b )
        {
            bool one = false;
            foreach( SqlToken t in @this )
            {
                if( one ) b.Append( separator );
                one = true;
                t.WriteWithoutTrivias( b );
            }
        }

        /// <summary>
        /// Returs a string for an <see cref="IEnumerable"/> of <see cref="SqlToken"/> without its trivias. 
        /// Calls <see cref="SqlToken.WriteWithoutTrivias"/> on each token.
        /// </summary>
        /// <param name="this">An <see cref="IEnumerable"/> of <see cref="SqlToken"/></param>
        /// <param name="separator">Separator between tokens.</param>
        /// <returns>Tokens without trivias.</returns>
        public static string ToStringWithoutTrivias( this IEnumerable<SqlToken> @this, string separator )
        {
            StringBuilder b = new StringBuilder();
            @this.WriteTokensWithoutTrivias( separator, b );
            return b.ToString();
        }

        /// <summary>
        /// Gets whether the <see cref="ISqlItem"/> is actually a <see cref="SqlToken"/> of a given type.
        /// </summary>
        /// <param name="this">Sql item.</param>
        /// <param name="type">The type of the token.</param>
        /// <returns>True on success.</returns>
        static public bool IsToken( this ISqlItem @this, SqlTokenType type )
        {
            SqlToken id = @this as SqlToken;
            return id != null && id.TokenType == type;
        }

        /// <summary>
        /// True if this <see cref="SqlTokenType"/> denotes a reserved keyword that starts a statement (select, create, declare, etc.)
        /// or a standard identifer that also can start a statement (throw, get, move, etc.).
        /// </summary>
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
        static public bool IsStartStatementOrWith( this SqlTokenType type )
        {
            return IsStartStatement( type ) || type == SqlTokenType.With;
        }


    }
}
