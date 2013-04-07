using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer
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
            foreach( var t in @this )
            {
                if( one ) b.Append( separator );
                one = true;
                t.WriteWithoutTrivias( b );
            }
        }

        /// <summary>
        /// Gets the number of enclosing parenthesis around the <see cref="ISqlExprEnclosable.ComponentsWithoutParenthesis"/>.
        /// </summary>
        public static int ParenthesisCount( this ISqlExprEnclosable @this )
        {
            return @this.Opener.Count;
        }
    }
}
